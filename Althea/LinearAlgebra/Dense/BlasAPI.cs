using System;
using System.Collections.Generic;


namespace Althea.LinearAlgebra.Dense
{
	/// <summary>
	/// The abstract class for runtime dense linear algebra API routines 
	/// </summary>
	public abstract partial class AbstractApi : AbstractApiSelector
	{
		#region basic
		/// <summary>
		/// Get the currently using <see cref="AbstractApi"/>.
		/// </summary>
		/// <remarks><b>DO NOT</b> invoke methods of this property directly unless you are sure about what you are doing; otherwise, there may be exceptions and / or unnoticeable bugs.</remarks>
		public static AbstractApi? Current => RecentAPIs.First?.Value;

		private static readonly LinkedList<AbstractApi> RecentAPIs = new();

		/// <summary>
		/// Set the currently using <see cref="AbstractApi"/> to the given <paramref name="implementation"/>
		/// </summary>
		/// <param name="implementation">The <see cref="Type"/> of the given implementation of <see cref="AbstractApi"/></param>
		/// <returns>Success or not.</returns>
		public static bool SetImplementation(Type implementation) => SetImplementation(RecentAPIs, implementation);

		/// <summary>
		/// Set the currently using <see cref="AbstractApi"/> to the given <paramref name="implementation"/>
		/// </summary>
		/// <param name="implementation">The instance of an implementation of <see cref="AbstractApi"/></param>
		/// <returns>Success or not.</returns>
		internal static bool SetImplementation(AbstractApi? implementation) => SetImplementation(RecentAPIs, implementation);
		#endregion


		#region support information
		/// <summary>
		/// When implemented by a derived class, check if the given <paramref name="location"/> is supported by vector unary operations of this implementation or not.
		/// </summary>
		/// <param name="location">The given <see cref="CombinationOfLocations"/></param>
		/// <returns>Whether vector unary operation on <paramref name="location"/> is supported by this <see cref="AbstractApi"/>.</returns>
		/// <remarks>
		/// The operations:
		/// <list type="bullet">
		/// <item><see cref="Scale{T}(Storage{T}, int, T)"/></item>
		/// <item><see cref="PointWisePower{T}(Storage{T}, int, T)"/></item>
		/// <item>etc.</item>
		/// </list>
		/// </remarks>
		protected abstract bool IsSupportedVectorUnary(CombinationOfLocations location);

		/// <summary>
		/// When implemented by a derived class, check if the given <see cref="CombinationOfLocations"/>s are supported by vector binary operations of this implementation or not.
		/// </summary>
		/// <param name="location1">The first given <see cref="CombinationOfLocations"/></param>
		/// <param name="location2">The second given <see cref="CombinationOfLocations"/></param>
		/// <returns>Whether binary operations on <paramref name="location1"/> and <paramref name="location2"/> are supported by this <see cref="AbstractApi"/>.</returns>
		/// <remarks>
		/// The operations:
		/// <list type="bullet">
		/// <item><see cref="VectorGeneralAdd{T}(T, Storage{T}, int, Storage{T}, int)"/></item>
		/// <item><see cref="PointWiseMultiply{T}(Storage{T}, int, Storage{T}, int)"/></item>
		/// <item><see cref="PointWiseCast{T, TOut}(Storage{T}, int, Storage{TOut}, int)"/></item>
		/// <item>etc.</item>
		/// </list>
		/// </remarks>
		protected abstract bool IsSupportedVectorBinary(CombinationOfLocations location1, CombinationOfLocations location2);

		/// <summary>
		/// When implemented by a derived class, check if the given <see cref="CombinationOfLocations"/>s are supported by vector unary and matrix unary operations of this implementation or not.
		/// </summary>
		/// <param name="vector">The given <see cref="CombinationOfLocations"/> of the vector</param>
		/// <param name="matrix">The given <see cref="CombinationOfLocations"/> of the matrix</param>
		/// <returns>Whether binary operations on <paramref name="vector"/> and <paramref name="matrix"/> are supported by this <see cref="AbstractApi"/>.</returns>
		/// <remarks>
		/// The operations:
		/// <list type="bullet">
		/// <item><see cref="SymmHermRankOneUpdate{T}(bool, bool, long, T, Storage{T}, int, T, Storage{T}, long)"/></item>
		/// <item>etc.</item>
		/// </list>
		/// </remarks>
		protected abstract bool IsSupportedVectorUnaryMatrixUnary(CombinationOfLocations vector, CombinationOfLocations matrix);

		/// <summary>
		/// When implemented by a derived class, check if the given <see cref="CombinationOfLocations"/>s are supported by binary vector and unary matrix operations of this implementation or not.
		/// </summary>
		/// <param name="vector1">The given <see cref="CombinationOfLocations"/> of the first vector</param>
		/// <param name="vector2">The given <see cref="CombinationOfLocations"/> of the second vector</param>
		/// <param name="matrix">The given <see cref="CombinationOfLocations"/> of matrix</param>
		/// <returns>Whether binary vector and unary matrix operations on <paramref name="vector1"/> and <paramref name="vector2"/> and <paramref name="matrix"/> are supported by this <see cref="AbstractApi"/>.</returns>
		/// <remarks>
		/// The operations:
		/// <list type="bullet">
		/// <item><see cref="GeneralMatrixMultiplyVector{T}(MatrixOperation, long, long, T, Storage{T}, long, Storage{T}, int, T, Storage{T}, int)"/></item>
		/// <item><see cref="SymmHermMatrixMultiplyVector{T}(bool, bool, long, T, Storage{T}, long, Storage{T}, int, T, Storage{T}, int)"/></item>
		/// <item><see cref="GenralRankOneUpdate{T}(bool, long, long, T, Storage{T}, int, Storage{T}, int, T, Storage{T}, long)"/></item>
		/// <item>etc.</item>
		/// </list>
		/// </remarks>
		protected abstract bool IsSupportedVectorBinaryMatrixUnary(CombinationOfLocations vector1, CombinationOfLocations vector2, CombinationOfLocations matrix);

		/// <summary>
		/// When implemented by a derived class, check if the given <see cref="CombinationOfLocations"/>s are supported by matrix binary operations of this implementation or not.
		/// </summary>
		/// <param name="location1">The first given <see cref="CombinationOfLocations"/></param>
		/// <param name="location2">The second given <see cref="CombinationOfLocations"/></param>
		/// <returns>Whether binary operations on <paramref name="location1"/> and <paramref name="location2"/> are supported by this <see cref="AbstractApi"/>.</returns>
		/// <remarks>
		/// The operations:
		/// <list type="bullet">
		/// <item><see cref="RankKUpdate{T}(bool, MatrixOperation, bool, long, long, T, Storage{T}, long, T, Storage{T}, long)"/></item>
		/// <item><see cref="GeneralMatricesAdd{T}(MatrixOperation, MatrixOperation, long, long, T, Storage{T}?, long, T, Storage{T}?, long, Storage{T}, long)"/></item>
		/// <item>etc.</item>
		/// </list>
		/// </remarks>
		protected abstract bool IsSupportedMatrixBinary(CombinationOfLocations location1, CombinationOfLocations location2);

		/// <summary>
		/// When implemented by a derived class, check if the given <see cref="CombinationOfLocations"/>s are supported by matrix trinary operations of this implementation or not.
		/// </summary>
		/// <param name="location1">The first given <see cref="CombinationOfLocations"/></param>
		/// <param name="location2">The second given <see cref="CombinationOfLocations"/></param>
		/// <param name="location3">The third given <see cref="CombinationOfLocations"/></param>
		/// <returns>Whether trinary operations on <paramref name="location1"/> and <paramref name="location2"/> are supported by this <see cref="AbstractApi"/>.</returns>
		/// <remarks>
		/// The operations:
		/// <list type="bullet">
		/// <item><see cref="GeneralMatricesMultiply{T}(MatrixOperation, MatrixOperation, long, long, long, T, Storage{T}, long, Storage{T}, long, T, Storage{T}, long)"/></item>
		/// <item><see cref="GeneralMatricesAdd{T}(MatrixOperation, MatrixOperation, long, long, T, Storage{T}?, long, T, Storage{T}?, long, Storage{T}, long)"/></item>
		/// <item><see cref="MatrixKronecker{T}(long, long, long, long, T, Storage{T}, long, Storage{T}, long, T, Storage{T}, long)"/></item>
		/// <item>etc.</item>
		/// </list>
		/// </remarks>
		protected abstract bool IsSupportedMatrixTrinary(CombinationOfLocations location1, CombinationOfLocations location2, CombinationOfLocations location3);
		#endregion


		#region static methods as dispatchers
		#region BLAS level 1
		/// <summary>
		/// Find the (smallest) index of the element with the maximum magnitude.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="x">The vector of type <typeparamref name="T"/></param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <returns>The resulting index</returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> is less than 1</exception>
		public static long AbsoluteValueArgMax<T>(IStorage x, int strideX) where T : unmanaged, INumber<T>
		{
			CombinationOfLocations location = x.LocationDescription;
			long result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedVectorUnary(location), node);
				success = node.Value.AbsoluteValueArgMax<T>(x, strideX, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// Find the (smallest) index of the element with the minimum magnitude.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="x">The vector of type <typeparamref name="T"/></param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <returns>The resulting index or 0 if <paramref name="strideX"/> is less than 1</returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> is less than 1</exception>
		public static long AbsoluteValueArgMin<T>(Storage<T> x, int strideX) where T : unmanaged, INumber<T>
		{
			CombinationOfLocations location = x.LocationDescription;
			long result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedVectorUnary(location), node);
				success = node.Value.AbsoluteValueArgMin_(x, strideX, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// Compute the sum of the absolute values of the elements of vector <paramref name="x"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="x">The vector of type <typeparamref name="T"/></param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <returns>The result value as a <see cref="double"/></returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> is less than 1</exception>
		public static double AbsoluteValueSum<T>(Storage<T> x, int strideX) where T : unmanaged, INumber<T>
		{
			CombinationOfLocations location = x.LocationDescription;
			double result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedVectorUnary(location), node);
				success = node.Value.AbsoluteValueSum_(x, strideX, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// Compute the Euclidean norm (2-norm) of the vector <paramref name="x"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="x">The vector of type <typeparamref name="T"/></param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <returns>The result value as a <see cref="double"/>, or 0 if <paramref name="strideX"/> ≤ 0</returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> is less than 1</exception>
		public static double Norm<T>(Storage<T> x, int strideX) where T : unmanaged, INumber<T>
		{
			CombinationOfLocations location = x.LocationDescription;
			double result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedVectorUnary(location), node);
				success = node.Value.Norm_(x, strideX, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// In-place scale the vector <paramref name="x"/> by <paramref name="scalar"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="x">The vector of type <typeparamref name="T"/></param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="scalar">The scalar used for multiplication</param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> is less than 1</exception>
		public static void Scale<T>(Storage<T> x, int strideX, T scalar) where T : unmanaged, INumber<T>
		{
			CombinationOfLocations location = x.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedVectorUnary(location), node);
				success = node.Value.Scale_(x, strideX, scalar);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Multiply the vector <paramref name="x"/> by the scalar <paramref name="α"/> and in-place add it to the vector <paramref name="y"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="α">The scalar used for multiplication</param>
		/// <param name="x">The vector of type <typeparamref name="T"/></param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The another vector of type <typeparamref name="T"/></param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="y"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> is less than 1</exception>
		public static void VectorGeneralAdd<T>(T α, Storage<T> x, int strideX, Storage<T> y, int strideY) where T : unmanaged, INumber<T>
		{
			CombinationOfLocations location1 = x.LocationDescription, location2 = y.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedVectorBinary(location1, location2), node);
				success = node.Value.VectorGeneralAdd_(α, x, strideX, y, strideY);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Compute the dot (inner) product of vectors <paramref name="x"/> and <paramref name="y"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="conjX">Conjugate <paramref name="x"/> or not</param>
		/// <param name="x">The vector of type <typeparamref name="T"/></param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The another vector of type <typeparamref name="T"/></param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="y"/></param>
		/// <returns>The result value as a <typeparamref name="T"/></returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> is less than 1</exception>
		public static T Dot<T>(bool conjX, Storage<T> x, int strideX, Storage<T> y, int strideY) where T : unmanaged, INumber<T>
		{
			CombinationOfLocations location1 = x.LocationDescription, location2 = y.LocationDescription;
			T result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedVectorBinary(location1, location2), node);
				success = node.Value.Dot_(conjX, x, strideX, y, strideY, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}
		#endregion

		#region BLAS level 2
		/// <summary>
		/// Perform the matrix-vector multiplication: <paramref name="y"/> = <paramref name="α"/> * <paramref name="op"/>(<paramref name="A"/>)* <paramref name="x"/> + <paramref name="β"/> * <paramref name="y"/>.<br/>
		/// Where <paramref name="A"/> is a <paramref name="m"/>×<paramref name="n"/> matrix stored in column-major format, <paramref name="x"/> and <paramref name="y"/> are vectors, and <paramref name="α"/> and <paramref name="β"/> are scalars.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="op">The <see cref="MatrixOperation"/> that is non- or (conj.) transpose</param>
		/// <param name="m">The number of rows of matrix <paramref name="A"/></param>
		/// <param name="n">The number of columns of matrix <paramref name="A"/></param>
		/// <param name="α">The scalar to be multiplied to <paramref name="A"/></param>
		/// <param name="A">The input array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(0, <paramref name="m"/>)</c></param>
		/// <param name="lda">The leading dimension of the two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="x">The vector of length at least <c>(1+(<paramref name="n"/>-1)*<paramref name="strideX"/>)</c> elements if <paramref name="op"/>==<see cref="MatrixOperation.None"/> or <c>(1+(<paramref name="m"/>-1)*<paramref name="strideX"/>)</c> otherwise</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="β">The scalar to be multiplied to <paramref name="y"/>. If this is 0, then the original values of <paramref name="y"/> will be ignored.</param>
		/// <param name="y">The input and output vector at least <c>(1+(<paramref name="m"/>-1)*<paramref name="strideY"/>)</c> elements if <paramref name="op"/>==<see cref="MatrixOperation.None"/> or <c>(1+(<paramref name="n"/>-1)*<paramref name="strideY"/>)</c> otherwise</param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="y"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> is less than 1</exception>
		public static void GeneralMatrixMultiplyVector<T>(MatrixOperation op, long m, long n, T α, Storage<T> A, long lda, Storage<T> x, int strideX, T β, Storage<T> y, int strideY) where T : unmanaged, INumber<T>
		{
			CombinationOfLocations location1 = x.LocationDescription, location2 = y.LocationDescription, locationMat = A.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedVectorBinaryMatrixUnary(location1, location2, locationMat), node);
				success = node.Value.GeneralMatrixMultiplyVector_(op, m, n, α, A, lda, x, strideX, β, y, strideY);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Perform the symmetric/hermitian matrix-vector multiplication:<br/>
		/// <c><paramref name="y"/> = <paramref name="α"/> * <paramref name="A"/>*<paramref name="x"/> + <paramref name="β"/> * <paramref name="y"/></c>.<br/>
		/// Where <paramref name="A"/> is a <paramref name="n"/>×<paramref name="n"/> symmetric/hermitian matrix stored in column-major format, <paramref name="x"/> is a vector, and <paramref name="α"/> is a scalar.
		/// </summary>
		/// <param name="fillUpper">The indicates whether <paramref name="A"/>'s upper or lower part is stored</param>
		/// <param name="hermA">Whether <paramref name="A"/> is a hermitian or a symmetric matrix</param>
		/// <param name="n">The number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="α">The scalar used for multiplication</param>
		/// <param name="A">The array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(0, <paramref name="n"/>)</c></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="x">The vector with <c>(1+(<paramref name="n"/>-1)*abs(<paramref name="strideX"/>))</c> elements</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="β">The scalar to be multiplied to <paramref name="y"/>. If this is 0, then the original values of <paramref name="y"/> will be ignored.</param>
		/// <param name="y">The input and output vector at least <c>(1+(<paramref name="n"/>-1)*abs(<paramref name="strideY"/>))</c></param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="y"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> is less than 1</exception>
		public static void SymmHermMatrixMultiplyVector<T>(bool fillUpper, bool hermA, long n, T α, Storage<T> A, long lda, Storage<T> x, int strideX, T β, Storage<T> y, int strideY) where T : unmanaged, INumber<T>
		{
			CombinationOfLocations location1 = x.LocationDescription, location2 = y.LocationDescription, locationMat = A.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedVectorBinaryMatrixUnary(location1, location2, locationMat), node);
				success = node.Value.SymmHermMatrixMultiplyVector_(fillUpper, hermA, n, α, A, lda, x, strideX, β, y, strideY);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Perform the rank-1 update:<br/>
		/// <c><paramref name="A"/> = <paramref name="α"/> * <paramref name="x"/> * <paramref name="y"/>^op + <paramref name="β"/> * <paramref name="A"/></c>, <c>op = <paramref name="conjY"/> ? H : T</c>.<br/>
		/// Where <paramref name="A"/> is a <paramref name="m"/>×<paramref name="n"/> matrix stored in column-major format, <paramref name="x"/> and <paramref name="y"/> are vectors, and <paramref name="α"/> is a scalar.
		/// </summary>
		/// <param name="conjY">Conjugate <paramref name="y"/> or not</param>
		/// <param name="m">The number of rows of matrix <paramref name="A"/></param>
		/// <param name="n">The number of columns of matrix <paramref name="A"/></param>
		/// <param name="α">The scalar used for multiplication</param>
		/// <param name="x">The vector with <c>(1+(<paramref name="m"/>-1)*<paramref name="strideX"/>)</c> elements</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The vector with <c>(1+(<paramref name="n"/>-1)*<paramref name="strideY"/>)</c> elements</param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="y"/></param>
		/// <param name="β">The scalar to be multiplied to <paramref name="A"/>. If this is 0, then the original values of <paramref name="A"/> will be ignored.</param>
		/// <param name="A">The input and output array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(0, <paramref name="m"/>)</c></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> is less than 1</exception>
		public static void GenralRankOneUpdate<T>(bool conjY, long m, long n, T α, Storage<T> x, int strideX, Storage<T> y, int strideY, T β, Storage<T> A, long lda) where T : unmanaged, INumber<T>
		{
			CombinationOfLocations location1 = x.LocationDescription, location2 = y.LocationDescription, locationMat = A.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedVectorBinaryMatrixUnary(location1, location2, locationMat), node);
				success = node.Value.GenralRankOneUpdate_(conjY, m, n, α, x, strideX, y, strideY, β, A, lda);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Perform the symmetric/hermitian rank-1 update:<br/>
		/// <c><paramref name="A"/> = <paramref name="α"/> * <paramref name="x"/> * <paramref name="x"/>^op + <paramref name="A"/></c>, <c>op = <paramref name="conjX"/> ? H : T</c>.<br/>
		/// Where <paramref name="A"/> is a <paramref name="n"/>×<paramref name="n"/> symmetric/hermitian matrix stored in column-major format, <paramref name="x"/> is a vector, and <paramref name="α"/> is a scalar.
		/// </summary>
		/// <param name="fillUpper">Whether the result symmetric matrix <paramref name="A"/> shall be stored in its upper or the lower part</param>
		/// <param name="conjX">Conjugate the second <paramref name="x"/> or not</param>
		/// <param name="n">The number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="α">The scalar used for multiplication</param>
		/// <param name="x">The vector with <c>(1+(<paramref name="n"/>-1)*<paramref name="strideX"/>)</c> elements</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="β">The scalar to be multiplied to <paramref name="A"/>. If this is 0, then the original values of <paramref name="A"/> will be ignored.</param>
		/// <param name="A">The array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(0, <paramref name="n"/>)</c></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> is less than 1</exception>
		public static void SymmHermRankOneUpdate<T>(bool fillUpper, bool conjX, long n, T α, Storage<T> x, int strideX, T β, Storage<T> A, long lda) where T : unmanaged, INumber<T>
		{
			CombinationOfLocations locationVec = x.LocationDescription, locationMat = A.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedVectorUnaryMatrixUnary(locationVec, locationMat), node);
				success = node.Value.SymmHermRankOneUpdate_(fillUpper, conjX, n, α, x, strideX, β, A, lda);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Perform the symmetric/hermitian rank-2 update:<br/>
		/// <c><paramref name="A"/> = <paramref name="α"/> * (<paramref name="x"/> * <paramref name="y"/>^op + <paramref name="x"/>^op * <paramref name="y"/>) + <paramref name="β"/> * <paramref name="A"/></c>, <c>op = <paramref name="conjugate"/> ? H : T</c>.<br/>
		/// Where <paramref name="A"/> is a <paramref name="n"/>×<paramref name="n"/> symmetric/hermitian matrix stored in column-major format, <paramref name="x"/> is a vector, and <paramref name="α"/> is a scalar.
		/// </summary>
		/// <param name="fillUpper">Whether the result symmetric matrix <paramref name="A"/> shall be stored in its upper or the lower part</param>
		/// <param name="conjugate">Conjugate the vectors <paramref name="x"/> and <paramref name="y"/> or not</param>
		/// <param name="n">The number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="α">The scalar used for multiplication</param>
		/// <param name="x">The left vector with <c>(1+(<paramref name="n"/>-1)*<paramref name="strideX"/>)</c> elements</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The right vector with <c>(1+(<paramref name="n"/>-1)*<paramref name="strideY"/>)</c> elements</param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="y"/></param>
		/// <param name="β">The scalar to be multiplied to <paramref name="A"/>. If this is 0, then the original values of <paramref name="A"/> will be ignored.</param>
		/// <param name="A">The array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(0, <paramref name="n"/>)</c></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> is less than 1</exception>
		public static void SymmHermRankTwoUpdate<T>(bool fillUpper, bool conjugate, long n, T α, Storage<T> x, int strideX, Storage<T> y, int strideY, T β, Storage<T> A, long lda) where T : unmanaged, INumber<T>
		{
			CombinationOfLocations locationVec1 = x.LocationDescription, locationVec2 = y.LocationDescription, locationMat = A.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedVectorBinaryMatrixUnary(locationVec1, locationVec2, locationMat), node);
				success = node.Value.SymmHermRankTwoUpdate_(fillUpper, conjugate, n, α, x, strideX, y, strideY, β, A, lda);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Perform the triangular matrix multiply:<br/>
		/// <c><paramref name="x"/> = <paramref name="op"/>(<paramref name="A"/>) * <paramref name="x"/></c><br/>
		/// Where <paramref name="A"/> is a <paramref name="n"/>×<paramref name="n"/> upper/lower triangular matrix stored in column-major format and <paramref name="x"/> is a vector.
		/// </summary>
		/// <param name="fillUpper">Whether <paramref name="A"/> is upper or lower triangular</param>
		/// <param name="unitDiag">Whether the diagonal elements of <paramref name="A"/> are all unit (1) or not</param>
		/// <param name="op">The <see cref="MatrixOperation"/> to indicate the simple operation of <paramref name="A"/></param>
		/// <param name="n">The number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="x">The vector with <c>(1+(<paramref name="n"/>-1)*<paramref name="strideX"/>)</c> elements</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="A">The array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(0, <paramref name="n"/>)</c></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> is less than 1</exception>
		public static void TriangularMatrixMultiplyVector<T>(bool fillUpper, bool unitDiag, MatrixOperation op, long n, Storage<T> A, long lda, Storage<T> x, int strideX) where T : unmanaged, INumber<T>
		{
			CombinationOfLocations locationVec = x.LocationDescription, locationMat = A.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedVectorUnaryMatrixUnary(locationVec, locationMat), node);
				success = node.Value.TriangularMatrixMultiplyVector_(fillUpper, unitDiag, op, n, A, lda, x, strideX);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}
		#endregion

		#region BLAS level 3
		/// <summary>
		/// Perform the matrix-matrix multiplication:<br/>
		/// <paramref name="C"/> = <paramref name="α"/> * <paramref name="opA"/>(<paramref name="A"/>) * <paramref name="opB"/>(<paramref name="B"/>) + <paramref name="β"/> * <paramref name="C"/>.<br/>
		/// Where <paramref name="α"/> and <paramref name="β"/> are scalars; <paramref name="A"/>, <paramref name="B"/> and <paramref name="C"/> are matrices stored in column-major format with dimensions <paramref name="opA"/>(<paramref name="A"/>) → <paramref name="m"/>×<paramref name="k"/>, <paramref name="opB"/>(<paramref name="B"/>) → <paramref name="k"/>×<paramref name="n"/> and <paramref name="C"/> → <paramref name="m"/>×<paramref name="n"/>, respectively.
		/// </summary>
		/// <param name="opA">The <see cref="MatrixOperation"/> to indicate the simple operation of <paramref name="A"/></param>
		/// <param name="opB">The <see cref="MatrixOperation"/> to indicate the simple operation of <paramref name="B"/></param>
		/// <param name="m">The number of rows of matrix <paramref name="opA"/>(<paramref name="A"/>) and <paramref name="C"/></param>
		/// <param name="n">The number of columns of matrix <paramref name="opB"/>(<paramref name="B"/>) and <paramref name="C"/></param>
		/// <param name="k">The number of columns of <paramref name="opA"/>(<paramref name="A"/>) and rows of <paramref name="opB"/>(<paramref name="B"/>)</param>
		/// <param name="α">The scalar to be multiplied to <paramref name="opA"/>(<paramref name="A"/>) * <paramref name="opB"/>(<paramref name="B"/>)</param>
		/// <param name="A">The array of dimensions <c><paramref name="lda"/>×<paramref name="k"/></c> with <c><paramref name="lda"/> ≥ max(0, <paramref name="m"/>)</c> if <paramref name="opA"/> == <see cref="MatrixOperation.None"/>, and <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(0, <paramref name="k"/>)</c> otherwise</param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store the matrix <paramref name="A"/></param>
		/// <param name="B">The array of dimension <c><paramref name="ldb"/>×<paramref name="n"/></c> with <c><paramref name="ldb"/> ≥ max(0, <paramref name="k"/>)</c> if <paramref name="opB"/> == <see cref="MatrixOperation.None"/>, and <c><paramref name="ldb"/>×<paramref name="k"/></c> with <c><paramref name="lda"/> ≥ max(0, <paramref name="n"/>)</c> otherwise</param>
		/// <param name="ldb">The leading dimension of two-dimensional array used to store matrix <paramref name="B"/></param>
		/// <param name="β">The scalar to be multiplied to <paramref name="C"/>. If this is 0, the original values of <paramref name="C"/> will be ignored.</param>
		/// <param name="C">The array of dimensions <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(0, <paramref name="m"/>)</c></param>
		/// <param name="ldc">The leading dimension of a two-dimensional array used to store the matrix <paramref name="C"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		public static void GeneralMatricesMultiply<T>(MatrixOperation opA, MatrixOperation opB, long m, long n, long k, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc) where T : unmanaged, INumber<T>
		{
			CombinationOfLocations location1 = A.LocationDescription, location2 = B.LocationDescription, location3 = C.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedMatrixTrinary(location1, location2, location3), node);
				success = node.Value.GeneralMatricesMultiply_(opA, opB, m, n, k, α, A, lda, B, ldb, β, C, ldc);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Perform the symmetric/hermitian matrix-matrix multiplication:<br/>
		/// If <paramref name="leftA"/> is true, <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="A"/> * <paramref name="B"/> + <paramref name="β"/> * <paramref name="C"/></c>; otherwise, <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="B"/> * <paramref name="A"/> + <paramref name="β"/> * <paramref name="C"/></c>.<br/>
		/// Where <paramref name="A"/> is a symmetric/hermitian matrix stored in lower or upper mode, <paramref name="B"/> and <paramref name="C"/> are <paramref name="m"/>×<paramref name="n"/> matrices, and <paramref name="α"/> and <paramref name="β"/> are scalars.
		/// </summary>
		/// <param name="fillUpper">The <see cref="bool"/> indicates whether matrix <paramref name="A"/> upper or lower part is stored</param>
		/// <param name="leftA">The <see cref="bool"/> indicates whether matrix <paramref name="A"/> is on the left or right of <paramref name="B"/></param>
		/// <param name="hermA">Whether <paramref name="A"/> is a hermitian or symmetric matrix</param>
		/// <param name="m">The number of rows of matrix <paramref name="C"/> and <paramref name="B"/>, with matrix <paramref name="A"/> sized accordingly</param>
		/// <param name="n">The number of columns of matrix <paramref name="C"/> and <paramref name="B"/>, with matrix <paramref name="A"/> sized accordingly</param>
		/// <param name="α">The scalar to be multiplied to <paramref name="A"/></param>
		/// <param name="A">The symmetric/Hermitian matrix of dimension <c><paramref name="lda"/>×<paramref name="m"/></c> with <c><paramref name="lda"/> ≥ max(0, <paramref name="m"/>)</c> if <paramref name="leftA"/> is true, and <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(0, <paramref name="n"/>)</c> otherwise</param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="B">The array of dimension <c><paramref name="ldb"/>×<paramref name="n"/></c> with <c><paramref name="ldb"/> ≥ max(0, <paramref name="m"/>)</c></param>
		/// <param name="ldb">The leading dimension of two-dimensional array used to store matrix <paramref name="B"/></param>
		/// <param name="β">The scalar to be multiplied by <paramref name="C"/>. If it is 0, the original values of <paramref name="C"/> will be ignored.</param>
		/// <param name="C">The array of dimension <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(0, <paramref name="m"/>)</c></param>
		/// <param name="ldc">The leading dimension of two-dimensional array used to store matrix <paramref name="B"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		public static void SymmHermMatrixMultiplyGeneral<T>(bool fillUpper, bool leftA, bool hermA, long m, long n, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc) where T : unmanaged, INumber<T>
		{
			CombinationOfLocations location1 = A.LocationDescription, location2 = B.LocationDescription, location3 = C.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedMatrixTrinary(location1, location2, location3), node);
				success = node.Value.SymmHermMatrixMultiplyGeneral_(fillUpper, leftA, hermA, m, n, α, A, lda, B, ldb, β, C, ldc);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Perform the symmetric/hermitian rank-k update:<br/>
		/// <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="op"/>(<paramref name="A"/>) * <paramref name="op"/>(<paramref name="A"/>)^pow + <paramref name="β"/> * <paramref name="C"/></c>, <c>pow = <paramref name="conjA"/> ? H : T</c>.<br/>
		/// Where <paramref name="α"/> and <paramref name="β"/> are scalars, <paramref name="C"/> is a symmetric/hermitian matrix stored in lower or upper mode, and <paramref name="A"/> is a matrix with dimensions<c><paramref name="op"/>(<paramref name="A"/>) == <paramref name="n"/>×<paramref name="k"/></c>.
		/// </summary>
		/// <param name="fillUpper">The <see cref="bool"/> indicates whether matrix <paramref name="C"/>'s upper or lower part will be overwritten</param>
		/// <param name="op">The <see cref="MatrixOperation"/> indicates the simple operation to <paramref name="A"/></param>
		/// <param name="conjA">Conjugate transpose <paramref name="A"/> or just transpose <paramref name="A"/></param>
		/// <param name="n">The number of rows of matrix <paramref name="op"/>(<paramref name="A"/>) and <paramref name="C"/></param>
		/// <param name="k">The number of columns of matrix <paramref name="op"/>(<paramref name="A"/>)</param>
		/// <param name="α">The scalar to be multiplied to <paramref name="op"/>(<paramref name="A"/>) * <paramref name="op"/>(<paramref name="A"/>)^pow</param>
		/// <param name="A">The array of column major with leading dimension = <paramref name="lda"/></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/>, must be of at least its number of rows</param>
		/// <param name="β">The scalar to be multiplied by <paramref name="C"/>. If it is 0, the original values of <paramref name="C"/> will be ignored.</param>
		/// <param name="C">The symmetric/hermitian matrix of dimension <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(0, <paramref name="n"/>)</c></param>
		/// <param name="ldc">The leading dimension of two-dimensional array used to store matrix <paramref name="C"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="C"/> is null or invalid</exception>
		public static void RankKUpdate<T>(bool fillUpper, MatrixOperation op, bool conjA, long n, long k, T α, Storage<T> A, long lda, T β, Storage<T> C, long ldc) where T : unmanaged, INumber<T>
		{
			CombinationOfLocations location1 = A.LocationDescription, location2 = C.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedMatrixBinary(location1, location2), node);
				success = node.Value.RankKUpdate_(fillUpper, op, conjA, n, k, α, A, lda, β, C, ldc);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Perform the symmetric/hermitian rank-2k update:<br/>
		/// <c><paramref name="C"/> = <paramref name="α"/> * (<paramref name="op"/>(<paramref name="A"/>) * <paramref name="op"/>(<paramref name="B"/>)^pow + <paramref name="op"/>(<paramref name="A"/>)^pow * <paramref name="op"/>(<paramref name="B"/>)) + <paramref name="β"/> * <paramref name="C"/></c>, <c>pow = <paramref name="conjugate"/> ? H : T</c>.<br/>
		/// Where <paramref name="α"/> and <paramref name="β"/> are scalars, <paramref name="C"/> is a symmetric/hermitian matrix stored in lower or upper mode, and <paramref name="A"/> and <paramref name="B"/> are matrices with dimensions <c><paramref name="op"/>(<paramref name="A"/>) == <paramref name="op"/>(<paramref name="B"/>) == <paramref name="n"/>×<paramref name="k"/></c>.
		/// </summary>
		/// <param name="fillUpper">The <see cref="bool"/> indicates whether matrix <paramref name="C"/>'s upper or lower part will be overwritten</param>
		/// <param name="op">The <see cref="MatrixOperation"/> indicates the simple operation to <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="conjugate">Conjugate transpose <paramref name="A"/> and <paramref name="B"/> or just transpose</param>
		/// <param name="n">The number of rows of matrix <paramref name="op"/>(<paramref name="A"/>), <paramref name="op"/>(<paramref name="B"/>) and <paramref name="C"/></param>
		/// <param name="k">The number of columns of matrix <paramref name="op"/>(<paramref name="A"/>) and <paramref name="op"/>(<paramref name="B"/>)</param>
		/// <param name="α">The scalar to be multiplied to <paramref name="op"/>(<paramref name="A"/>) * <paramref name="op"/>(<paramref name="A"/>)^pow</param>
		/// <param name="A">The array of column major with leading dimension = <paramref name="lda"/></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/>, must be of at least its number of rows</param>
		/// <param name="B">The array of column major with leading dimension = <paramref name="ldb"/></param>
		/// <param name="ldb">The leading dimension of two-dimensional array used to store matrix <paramref name="B"/>, must be of at least its number of rows</param>
		/// <param name="β">The scalar to be multiplied by <paramref name="C"/>. If it is 0, the original values of <paramref name="C"/> will be ignored.</param>
		/// <param name="C">The symmetric/hermitian matrix of dimension <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(0, <paramref name="n"/>)</c></param>
		/// <param name="ldc">The leading dimension of two-dimensional array used to store matrix <paramref name="C"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="C"/> is null or invalid</exception>
		public static void RankTwoKUpdate<T>(bool fillUpper, MatrixOperation op, bool conjugate, long n, long k, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc) where T : unmanaged, INumber<T>
		{
			CombinationOfLocations location1 = A.LocationDescription, location2 = B.LocationDescription, location3 = C.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedMatrixTrinary(location1, location2, location3), node);
				success = node.Value.RankTwoKUpdate_(fillUpper, op, conjugate, n, k, α, A, lda, B, ldb, β, C, ldc);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Perform the variant symmetric/hermitian rank-k update:<br/>
		/// <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="op"/>(<paramref name="A"/>) * <paramref name="op"/>(<paramref name="B"/>)^pow + <paramref name="β"/> * <paramref name="C"/></c>, <c>pow = <paramref name="conjB"/> ? H : T</c>.<br/>
		/// Where <paramref name="α"/> and <paramref name="β"/> are scalars, <paramref name="C"/> is a symmetric/hermitian matrix stored in lower or upper mode, and <paramref name="A"/> and <paramref name="B"/> are matrices with dimensions <c><paramref name="op"/>(<paramref name="A"/>) == <paramref name="op"/>(<paramref name="B"/>) == <paramref name="n"/>×<paramref name="k"/></c>.<br/>
		/// This routine can be used when the matrix <paramref name="B"/> is in such way that the result is guaranteed to be hermitian. For example, <paramref name="B"/> is a column-wise scaling of <paramref name="A"/>.
		/// </summary>
		/// <param name="fillUpper">The <see cref="bool"/> indicates whether matrix <paramref name="C"/>'s upper or lower part will be overwritten</param>
		/// <param name="op">The <see cref="MatrixOperation"/> indicates the simple operation to <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="conjB">Conjugate transpose <paramref name="B"/> or just transpose <paramref name="B"/></param>
		/// <param name="n">The number of rows of matrix <paramref name="op"/>(<paramref name="A"/>), <paramref name="op"/>(<paramref name="B"/>) and <paramref name="C"/></param>
		/// <param name="k">The number of columns of matrix <paramref name="op"/>(<paramref name="A"/>) and <paramref name="op"/>(<paramref name="B"/>)</param>
		/// <param name="α">The scalar to be multiplied to <paramref name="op"/>(<paramref name="A"/>) * <paramref name="op"/>(<paramref name="A"/>)^pow</param>
		/// <param name="A">The array of column major with leading dimension = <paramref name="lda"/></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/>, must be of at least its number of rows</param>
		/// <param name="B">The array of column major with leading dimension = <paramref name="ldb"/></param>
		/// <param name="ldb">The leading dimension of two-dimensional array used to store matrix <paramref name="B"/>, must be of at least its number of rows</param>
		/// <param name="β">The scalar to be multiplied by <paramref name="C"/>. If it is 0, the original values of <paramref name="C"/> will be ignored.</param>
		/// <param name="C">The symmetric/hermitian matrix of dimension <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(0, <paramref name="n"/>)</c></param>
		/// <param name="ldc">The leading dimension of two-dimensional array used to store matrix <paramref name="C"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="C"/> is null or invalid</exception>
		public static void RankKUpdateVariant<T>(bool fillUpper, MatrixOperation op, bool conjB, long n, long k, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc) where T : unmanaged, INumber<T>
		{
			CombinationOfLocations location1 = A.LocationDescription, location2 = B.LocationDescription, location3 = C.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedMatrixTrinary(location1, location2, location3), node);
				success = node.Value.RankKUpdateVariant_(fillUpper, op, conjB, n, k, α, A, lda, B, ldb, β, C, ldc);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Solve the triangular linear systems with multiple right-hand-sides for <c>x</c> and overwrite it to <paramref name="B"/>:<br/>
		/// <c><paramref name="op"/>(<paramref name="A"/>) * x == <paramref name="α"/> * <paramref name="B"/></c> if <paramref name="leftA"/> is true, or <c>x * <paramref name="op"/>(<paramref name="A"/>) == <paramref name="α"/> * <paramref name="B"/></c> otherwise.
		/// </summary>
		/// <param name="leftA">Whether the matrix <paramref name="A"/> is at left side or right side</param>
		/// <param name="fillUpper">Whether the matrix <paramref name="A"/>'s upper or lower triangle is filled</param>
		/// <param name="unitDiag">Whether the matrix <paramref name="A"/>'s diagonal elements are all 1 or not</param>
		/// <param name="op">The <see cref="MatrixOperation"/> indicates the simple operation to <paramref name="A"/></param>
		/// <param name="m">The number of rows and columns of <paramref name="A"/> and number of rows of <paramref name="B"/></param>
		/// <param name="n">The number of columns of <paramref name="B"/>, i.e., the number of linear systems to be solved</param>
		/// <param name="α">The scalar to multiply to <paramref name="B"/></param>
		/// <param name="A">The input triangular matrix <paramref name="A"/> of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(0, <paramref name="m"/>)</c></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="B">The input/output right-hand-side matrix. Overwritten by the solutions at exit.</param>
		/// <param name="ldb">The leading dimension of two-dimensional array used to store matrix <paramref name="B"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		public static void TriangularMatrixSolve<T>(bool leftA, bool fillUpper, bool unitDiag, MatrixOperation op, long m, long n, T α, Storage<T> A, long lda, Storage<T> B, long ldb) where T : unmanaged, INumber<T>
		{
			CombinationOfLocations location1 = A.LocationDescription, location2 = B.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedMatrixBinary(location1, location2), node);
				success = node.Value.TriangularMatrixSolve_(leftA, fillUpper, unitDiag, op, m, n, α, A, lda, B, ldb);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}


		/// <summary>
		/// Multiply the triangular matrix <paramref name="A"/> with the given matrix <paramref name="B"/> and overwrite the result to <paramref name="C"/>:<br/>
		/// <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="op"/>(<paramref name="A"/>) * <paramref name="B"/></c> if <paramref name="leftA"/> is true, or <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="B"/> * <paramref name="op"/>(<paramref name="A"/>)</c> otherwise.
		/// </summary>
		/// <param name="leftA">Whether the matrix <paramref name="A"/> is at left side or right side</param>
		/// <param name="fillUpper">Whether the matrix <paramref name="A"/>'s upper or lower triangle is filled</param>
		/// <param name="unitDiag">Whether the matrix <paramref name="A"/>'s diagonal elements are all 1 or not</param>
		/// <param name="op">The <see cref="MatrixOperation"/> indicates the simple operation to <paramref name="A"/></param>
		/// <param name="m">The number of rows of <paramref name="B"/></param>
		/// <param name="n">The number of columns of <paramref name="B"/></param>
		/// <param name="α">The scalar to multiply to <paramref name="B"/></param>
		/// <param name="A">The input triangular matrix <paramref name="A"/> of dimension <c><paramref name="lda"/>×<paramref name="m"/></c> if <paramref name="leftA"/>, or <c><paramref name="lda"/>×<paramref name="n"/></c> otherwise</param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="B">The input general matrix of dimension <c><paramref name="ldb"/>×<paramref name="n"/></c></param>
		/// <param name="ldb">The leading dimension of two-dimensional array used to store matrix <paramref name="B"/></param>
		/// <param name="C">The output matrix to be overwritten by the result at exit, can be <paramref name="B"/> when <paramref name="ldc"/> == <paramref name="ldb"/>.</param>
		/// <param name="ldc">The leading dimension of two-dimensional array used to store matrix <paramref name="C"/>, must be <paramref name="ldb"/> when <paramref name="B"/> == <paramref name="C"/>.</param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		public static void TriangularMatrixMultiply<T>(bool leftA, bool fillUpper, bool unitDiag, MatrixOperation op, long m, long n, T α, Storage<T> A, long lda, Storage<T> B, long ldb, Storage<T> C, long ldc) where T : unmanaged, INumber<T>
		{
			CombinationOfLocations location1 = A.LocationDescription, location2 = B.LocationDescription, location3 = C.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedMatrixTrinary(location1, location2, location3), node);
				success = node.Value.TriangularMatrixMultiply_(leftA, fillUpper, unitDiag, op, m, n, α, A, lda, B, ldb, C, ldc);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}
		#endregion
		#endregion


		#region abstract methods that actually do computations
		#region BLAS level 1
		/// <summary>
		/// When implemented by a derived class, find the (smallest) index of the element with the maximum magnitude.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="x">The vector of type <typeparamref name="T"/></param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="index">Output the resulting index</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> is less than 1</exception>
		protected abstract bool AbsoluteValueArgMax<T>(IStorage x, int strideX, out long index) where T : unmanaged, INumber<T>;
		
		/// <summary>
		/// When implemented by a derived class, find the (smallest) index of the element with the minimum magnitude.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="x">The vector of type <typeparamref name="T"/></param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="index">Output the resulting index</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> is less than 1</exception>
		protected abstract bool AbsoluteValueArgMin_<T>(Storage<T> x, int strideX, out long index) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, compute the sum of the absolute values of the elements of vector <paramref name="x"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="x">The vector of type <typeparamref name="T"/></param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="sum">Output the result as a <see cref="double"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> is less than 1</exception>
		protected abstract bool AbsoluteValueSum_<T>(Storage<T> x, int strideX, out double sum) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, multiply the vector <paramref name="x"/> by the scalar <paramref name="α"/> and in-place add it to the vector <paramref name="y"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="α">The scalar used for multiplication</param>
		/// <param name="x">The vector of type <typeparamref name="T"/></param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The another vector of type <typeparamref name="T"/></param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="y"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> is less than 1</exception>
		protected abstract bool VectorGeneralAdd_<T>(T α, Storage<T> x, int strideX, Storage<T> y, int strideY) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, compute the dot (inner) product of vectors <paramref name="x"/> and <paramref name="y"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="conjX">Conjugate <paramref name="x"/> or not</param>
		/// <param name="x">The vector of type <typeparamref name="T"/></param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The another vector of type <typeparamref name="T"/></param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="y"/></param>
		/// <param name="dot">Output the result value as a <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> is less than 1</exception>
		protected abstract bool Dot_<T>(bool conjX, Storage<T> x, int strideX, Storage<T> y, int strideY, out T dot) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, compute the Euclidean norm (2-norm) of the vector <paramref name="x"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="x">The vector of type <typeparamref name="T"/></param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="norm">Output the result value as a <see cref="double"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> is less than 1</exception>
		protected abstract bool Norm_<T>(Storage<T> x, int strideX, out double norm) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, in-place scale the vector <paramref name="x"/> by <paramref name="scalar"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="x">The vector of type <typeparamref name="T"/></param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="scalar">The scalar used for multiplication</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> is less than 1</exception>
		protected abstract bool Scale_<T>(Storage<T> x, int strideX, T scalar) where T : unmanaged, INumber<T>;
		#endregion

		#region BLAS level 2
		/// <summary>
		/// When implemented by a derived class, perform the matrix-vector multiplication: <paramref name="y"/> = <paramref name="α"/> * <paramref name="op"/>(<paramref name="A"/>)* <paramref name="x"/> + <paramref name="β"/> * <paramref name="y"/>.<br/>
		/// Where <paramref name="A"/> is a <paramref name="m"/>×<paramref name="n"/> matrix stored in column-major format, <paramref name="x"/> and <paramref name="y"/> are vectors, and <paramref name="α"/> and <paramref name="β"/> are scalars.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="op">The <see cref="MatrixOperation"/> that is non- or (conj.) transpose</param>
		/// <param name="m">The number of rows of matrix <paramref name="A"/></param>
		/// <param name="n">The number of columns of matrix <paramref name="A"/></param>
		/// <param name="α">The scalar to be multiplied to <paramref name="A"/></param>
		/// <param name="A">The input array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(0, <paramref name="m"/>)</c></param>
		/// <param name="lda">The leading dimension of the two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="x">The vector of length at least <c>(1+(<paramref name="n"/>-1)*<paramref name="strideX"/>)</c> elements if <paramref name="op"/>==<see cref="MatrixOperation.None"/> or <c>(1+(<paramref name="m"/>-1)*<paramref name="strideX"/>)</c> otherwise</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="β">The scalar to be multiplied to <paramref name="y"/>. If this is 0, then the original values of <paramref name="y"/> will be ignored.</param>
		/// <param name="y">The input and output vector at least <c>(1+(<paramref name="m"/>-1)*<paramref name="strideY"/>)</c> elements if <paramref name="op"/>==<see cref="MatrixOperation.None"/> or <c>(1+(<paramref name="n"/>-1)*<paramref name="strideY"/>)</c> otherwise</param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="y"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> is less than 1</exception>
		protected abstract bool GeneralMatrixMultiplyVector_<T>(MatrixOperation op, long m, long n, T α, Storage<T> A, long lda, Storage<T> x, int strideX, T β, Storage<T> y, int strideY) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, perform the symmetric/hermitian matrix-vector multiplication:<br/>
		/// <c><paramref name="y"/> = <paramref name="α"/> * <paramref name="A"/>*<paramref name="x"/> + <paramref name="β"/> * <paramref name="y"/></c>.<br/>
		/// Where <paramref name="A"/> is a <paramref name="n"/>×<paramref name="n"/> symmetric/hermitian matrix stored in column-major format, <paramref name="x"/> is a vector, and <paramref name="α"/> is a scalar.
		/// </summary>
		/// <param name="fillUpper">The indicates whether <paramref name="A"/>'s upper or lower part is stored</param>
		/// <param name="hermA">Whether <paramref name="A"/> is a hermitian or a symmetric matrix</param>
		/// <param name="n">The number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="α">The scalar used for multiplication</param>
		/// <param name="A">The array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(0, <paramref name="n"/>)</c></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="x">The vector with <c>(1+(<paramref name="n"/>-1)*abs(<paramref name="strideX"/>))</c> elements</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="β">The scalar to be multiplied to <paramref name="y"/>. If this is 0, then the original values of <paramref name="y"/> will be ignored.</param>
		/// <param name="y">The input and output vector at least <c>(1+(<paramref name="n"/>-1)*abs(<paramref name="strideY"/>))</c></param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="y"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> is less than 1</exception>
		protected abstract bool SymmHermMatrixMultiplyVector_<T>(bool fillUpper, bool hermA, long n, T α, Storage<T> A, long lda, Storage<T> x, int strideX, T β, Storage<T> y, int strideY) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, perform the rank-1 update:<br/>
		/// <c><paramref name="A"/> = <paramref name="α"/> * <paramref name="x"/> * <paramref name="y"/>^op + <paramref name="β"/> * <paramref name="A"/></c>, <c>op = <paramref name="conjY"/> ? H : T</c>.<br/>
		/// Where <paramref name="A"/> is a <paramref name="m"/>×<paramref name="n"/> matrix stored in column-major format, <paramref name="x"/> and <paramref name="y"/> are vectors, and <paramref name="α"/> is a scalar.
		/// </summary>
		/// <param name="conjY">Conjugate <paramref name="y"/> or not</param>
		/// <param name="m">The number of rows of matrix <paramref name="A"/></param>
		/// <param name="n">The number of columns of matrix <paramref name="A"/></param>
		/// <param name="α">The scalar used for multiplication</param>
		/// <param name="x">The vector with <c>(1+(<paramref name="m"/>-1)*<paramref name="strideX"/>)</c> elements</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The vector with <c>(1+(<paramref name="n"/>-1)*<paramref name="strideY"/>)</c> elements</param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="y"/></param>
		/// <param name="β">The scalar to be multiplied to <paramref name="A"/>. If this is 0, then the original values of <paramref name="A"/> will be ignored.</param>
		/// <param name="A">The input and output array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(0, <paramref name="m"/>)</c></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> is less than 1</exception>
		protected abstract bool GenralRankOneUpdate_<T>(bool conjY, long m, long n, T α, Storage<T> x, int strideX, Storage<T> y, int strideY, T β, Storage<T> A, long lda) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, perform the symmetric/hermitian rank-1 update:<br/>
		/// <c><paramref name="A"/> = <paramref name="α"/> * <paramref name="x"/> * <paramref name="x"/>^op + <paramref name="β"/> * <paramref name="A"/></c>, <c>op = <paramref name="conjX"/> ? H : T</c>.<br/>
		/// Where <paramref name="A"/> is a <paramref name="n"/>×<paramref name="n"/> symmetric/hermitian matrix stored in column-major format, <paramref name="x"/> is a vector, and <paramref name="α"/> is a scalar.
		/// </summary>
		/// <param name="fillUpper">Whether the result symmetric matrix <paramref name="A"/> shall be stored in its upper or the lower part</param>
		/// <param name="conjX">Conjugate the second <paramref name="x"/> or not</param>
		/// <param name="n">The number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="α">The scalar used for multiplication</param>
		/// <param name="x">The vector with <c>(1+(<paramref name="n"/>-1)*<paramref name="strideX"/>)</c> elements</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="β">The scalar to be multiplied to <paramref name="A"/>. If this is 0, then the original values of <paramref name="A"/> will be ignored.</param>
		/// <param name="A">The array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(0, <paramref name="n"/>)</c></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> is less than 1</exception>
		protected abstract bool SymmHermRankOneUpdate_<T>(bool fillUpper, bool conjX, long n, T α, Storage<T> x, int strideX, T β, Storage<T> A, long lda) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, perform the symmetric/hermitian rank-2 update:<br/>
		/// <c><paramref name="A"/> = <paramref name="α"/> * (<paramref name="x"/> * <paramref name="y"/>^op + <paramref name="x"/>^op * <paramref name="y"/>) + <paramref name="β"/> * <paramref name="A"/></c>, <c>op = <paramref name="conjugate"/> ? H : T</c>.<br/>
		/// Where <paramref name="A"/> is a <paramref name="n"/>×<paramref name="n"/> symmetric/hermitian matrix stored in column-major format, <paramref name="x"/> is a vector, and <paramref name="α"/> is a scalar.
		/// </summary>
		/// <param name="fillUpper">Whether the result symmetric matrix <paramref name="A"/> shall be stored in its upper or the lower part</param>
		/// <param name="conjugate">Conjugate the vectors <paramref name="x"/> and <paramref name="y"/> or not</param>
		/// <param name="n">The number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="α">The scalar used for multiplication</param>
		/// <param name="x">The left vector with <c>(1+(<paramref name="n"/>-1)*<paramref name="strideX"/>)</c> elements</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The right vector with <c>(1+(<paramref name="n"/>-1)*<paramref name="strideY"/>)</c> elements</param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="y"/></param>
		/// <param name="β">The scalar to be multiplied to <paramref name="A"/>. If this is 0, then the original values of <paramref name="A"/> will be ignored.</param>
		/// <param name="A">The array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(0, <paramref name="n"/>)</c></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> is less than 1</exception>
		protected abstract bool SymmHermRankTwoUpdate_<T>(bool fillUpper, bool conjugate, long n, T α, Storage<T> x, int strideX, Storage<T> y, int strideY, T β, Storage<T> A, long lda) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, perform the triangular matrix multiply:<br/>
		/// <c><paramref name="x"/> = <paramref name="op"/>(<paramref name="A"/>) * <paramref name="x"/></c><br/>
		/// Where <paramref name="A"/> is a <paramref name="n"/>×<paramref name="n"/> upper/lower triangular matrix stored in column-major format and <paramref name="x"/> is a vector.
		/// </summary>
		/// <param name="fillUpper">Whether <paramref name="A"/> is upper or lower triangular</param>
		/// <param name="unitDiag">Whether the diagonal elements of <paramref name="A"/> are all unit (1) or not</param>
		/// <param name="op">The <see cref="MatrixOperation"/> to indicate the simple operation of <paramref name="A"/></param>
		/// <param name="n">The number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="x">The vector with <c>(1+(<paramref name="n"/>-1)*<paramref name="strideX"/>)</c> elements</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="A">The array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(0, <paramref name="n"/>)</c></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> is less than 1</exception>
		protected abstract bool TriangularMatrixMultiplyVector_<T>(bool fillUpper, bool unitDiag, MatrixOperation op, long n, Storage<T> A, long lda, Storage<T> x, int strideX) where T : unmanaged, INumber<T>;
		#endregion

		#region BLAS level 3
		/// <summary>
		/// When implemented by a derived class, perform the matrix-matrix multiplication:<br/>
		/// <paramref name="C"/> = <paramref name="α"/> * <paramref name="opA"/>(<paramref name="A"/>) * <paramref name="opB"/>(<paramref name="B"/>) + <paramref name="β"/> * <paramref name="C"/>.<br/>
		/// Where <paramref name="α"/> and <paramref name="β"/> are scalars; <paramref name="A"/>, <paramref name="B"/> and <paramref name="C"/> are matrices stored in column-major format with dimensions <paramref name="opA"/>(<paramref name="A"/>) → <paramref name="m"/>×<paramref name="k"/>, <paramref name="opB"/>(<paramref name="B"/>) → <paramref name="k"/>×<paramref name="n"/> and <paramref name="C"/> → <paramref name="m"/>×<paramref name="n"/>, respectively.
		/// </summary>
		/// <param name="opA">The <see cref="MatrixOperation"/> to indicate the simple operation of <paramref name="A"/></param>
		/// <param name="opB">The <see cref="MatrixOperation"/> to indicate the simple operation of <paramref name="B"/></param>
		/// <param name="m">The number of rows of matrix <paramref name="opA"/>(<paramref name="A"/>) and <paramref name="C"/></param>
		/// <param name="n">The number of columns of matrix <paramref name="opB"/>(<paramref name="B"/>) and <paramref name="C"/></param>
		/// <param name="k">The number of columns of <paramref name="opA"/>(<paramref name="A"/>) and rows of <paramref name="opB"/>(<paramref name="B"/>)</param>
		/// <param name="α">The scalar to be multiplied to <paramref name="opA"/>(<paramref name="A"/>) * <paramref name="opB"/>(<paramref name="B"/>)</param>
		/// <param name="A">The array of dimensions <c><paramref name="lda"/>×<paramref name="k"/></c> with <c><paramref name="lda"/> ≥ max(0, <paramref name="m"/>)</c> if <paramref name="opA"/> == <see cref="MatrixOperation.None"/>, and <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(0, <paramref name="k"/>)</c> otherwise</param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store the matrix <paramref name="A"/></param>
		/// <param name="B">The array of dimension <c><paramref name="ldb"/>×<paramref name="n"/></c> with <c><paramref name="ldb"/> ≥ max(0, <paramref name="k"/>)</c> if <paramref name="opB"/> == <see cref="MatrixOperation.None"/>, and <c><paramref name="ldb"/>×<paramref name="k"/></c> with <c><paramref name="lda"/> ≥ max(0, <paramref name="n"/>)</c> otherwise</param>
		/// <param name="ldb">The leading dimension of two-dimensional array used to store matrix <paramref name="B"/></param>
		/// <param name="β">The scalar to be multiplied to <paramref name="C"/>. If this is 0, the original values of <paramref name="C"/> will be ignored.</param>
		/// <param name="C">The array of dimensions <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(0, <paramref name="m"/>)</c></param>
		/// <param name="ldc">The leading dimension of a two-dimensional array used to store the matrix <paramref name="C"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		protected abstract bool GeneralMatricesMultiply_<T>(MatrixOperation opA, MatrixOperation opB, long m, long n, long k, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, perform the symmetric/hermitian matrix-matrix multiplication:<br/>
		/// If <paramref name="leftA"/> is true, <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="A"/> * <paramref name="B"/> + <paramref name="β"/> * <paramref name="C"/></c>; otherwise, <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="B"/> * <paramref name="A"/> + <paramref name="β"/> * <paramref name="C"/></c>.<br/>
		/// Where <paramref name="A"/> is a symmetric/hermitian matrix stored in lower or upper mode, <paramref name="B"/> and <paramref name="C"/> are <paramref name="m"/>×<paramref name="n"/> matrices, and <paramref name="α"/> and <paramref name="β"/> are scalars.
		/// </summary>
		/// <param name="fillUpper">The <see cref="bool"/> indicates whether matrix <paramref name="A"/> upper or lower part is stored</param>
		/// <param name="leftA">The <see cref="bool"/> indicates whether matrix <paramref name="A"/> is on the left or right of <paramref name="B"/></param>
		/// <param name="hermA">Whether <paramref name="A"/> is a hermitian or symmetric matrix</param>
		/// <param name="m">The number of rows of matrix <paramref name="C"/> and <paramref name="B"/>, with matrix <paramref name="A"/> sized accordingly</param>
		/// <param name="n">The number of columns of matrix <paramref name="C"/> and <paramref name="B"/>, with matrix <paramref name="A"/> sized accordingly</param>
		/// <param name="α">The scalar to be multiplied to <paramref name="A"/></param>
		/// <param name="A">The symmetric/Hermitian matrix of dimension <c><paramref name="lda"/>×<paramref name="m"/></c> with <c><paramref name="lda"/> ≥ max(0, <paramref name="m"/>)</c> if <paramref name="leftA"/> is true, and <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(0, <paramref name="n"/>)</c> otherwise</param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="B">The array of dimension <c><paramref name="ldb"/>×<paramref name="n"/></c> with <c><paramref name="ldb"/> ≥ max(0, <paramref name="m"/>)</c></param>
		/// <param name="ldb">The leading dimension of two-dimensional array used to store matrix <paramref name="B"/></param>
		/// <param name="β">The scalar to be multiplied by <paramref name="C"/>. If it is 0, the original values of <paramref name="C"/> will be ignored.</param>
		/// <param name="C">The array of dimension <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(0, <paramref name="m"/>)</c></param>
		/// <param name="ldc">The leading dimension of two-dimensional array used to store matrix <paramref name="B"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		protected abstract bool SymmHermMatrixMultiplyGeneral_<T>(bool fillUpper, bool leftA, bool hermA, long m, long n, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, perform the symmetric/hermitian rank-k update:<br/>
		/// <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="op"/>(<paramref name="A"/>) * <paramref name="op"/>(<paramref name="A"/>)^pow + <paramref name="β"/> * <paramref name="C"/></c>, <c>pow = <paramref name="conjA"/> ? H : T</c>.<br/>
		/// Where <paramref name="α"/> and <paramref name="β"/> are scalars, <paramref name="C"/> is a symmetric/hermitian matrix stored in lower or upper mode, and <paramref name="A"/> is a matrix with dimensions<c><paramref name="op"/>(<paramref name="A"/>) == <paramref name="n"/>×<paramref name="k"/></c>.
		/// </summary>
		/// <param name="fillUpper">The <see cref="bool"/> indicates whether matrix <paramref name="C"/>'s upper or lower part will be overwritten</param>
		/// <param name="op">The <see cref="MatrixOperation"/> indicates the simple operation to <paramref name="A"/></param>
		/// <param name="conjA">Conjugate transpose <paramref name="A"/> or just transpose <paramref name="A"/></param>
		/// <param name="n">The number of rows of matrix <paramref name="op"/>(<paramref name="A"/>) and <paramref name="C"/></param>
		/// <param name="k">The number of columns of matrix <paramref name="op"/>(<paramref name="A"/>)</param>
		/// <param name="α">The scalar to be multiplied to <paramref name="op"/>(<paramref name="A"/>) * <paramref name="op"/>(<paramref name="A"/>)^pow</param>
		/// <param name="A">The array of column major with leading dimension = <paramref name="lda"/></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/>, must be of at least its number of rows</param>
		/// <param name="β">The scalar to be multiplied by <paramref name="C"/>. If it is 0, the original values of <paramref name="C"/> will be ignored.</param>
		/// <param name="C">The symmetric/hermitian matrix of dimension <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(0, <paramref name="n"/>)</c></param>
		/// <param name="ldc">The leading dimension of two-dimensional array used to store matrix <paramref name="C"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="C"/> is null or invalid</exception>
		protected abstract bool RankKUpdate_<T>(bool fillUpper, MatrixOperation op, bool conjA, long n, long k, T α, Storage<T> A, long lda, T β, Storage<T> C, long ldc) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, perform the symmetric/hermitian rank-2k update:<br/>
		/// <c><paramref name="C"/> = <paramref name="α"/> * (<paramref name="op"/>(<paramref name="A"/>) * <paramref name="op"/>(<paramref name="B"/>)^pow + <paramref name="op"/>(<paramref name="A"/>)^pow * <paramref name="op"/>(<paramref name="B"/>)) + <paramref name="β"/> * <paramref name="C"/></c>, <c>pow = <paramref name="conjugate"/> ? H : T</c>.<br/>
		/// Where <paramref name="α"/> and <paramref name="β"/> are scalars, <paramref name="C"/> is a symmetric/hermitian matrix stored in lower or upper mode, and <paramref name="A"/> and <paramref name="B"/> are matrices with dimensions <c><paramref name="op"/>(<paramref name="A"/>) == <paramref name="op"/>(<paramref name="B"/>) == <paramref name="n"/>×<paramref name="k"/></c>.
		/// </summary>
		/// <param name="fillUpper">The <see cref="bool"/> indicates whether matrix <paramref name="C"/>'s upper or lower part will be overwritten</param>
		/// <param name="op">The <see cref="MatrixOperation"/> indicates the simple operation to <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="conjugate">Conjugate transpose <paramref name="A"/> and <paramref name="B"/> or just transpose</param>
		/// <param name="n">The number of rows of matrix <paramref name="op"/>(<paramref name="A"/>), <paramref name="op"/>(<paramref name="B"/>) and <paramref name="C"/></param>
		/// <param name="k">The number of columns of matrix <paramref name="op"/>(<paramref name="A"/>) and <paramref name="op"/>(<paramref name="B"/>)</param>
		/// <param name="α">The scalar to be multiplied to <paramref name="op"/>(<paramref name="A"/>) * <paramref name="op"/>(<paramref name="A"/>)^pow</param>
		/// <param name="A">The array of column major with leading dimension = <paramref name="lda"/></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/>, must be of at least its number of rows</param>
		/// <param name="B">The array of column major with leading dimension = <paramref name="ldb"/></param>
		/// <param name="ldb">The leading dimension of two-dimensional array used to store matrix <paramref name="B"/>, must be of at least its number of rows</param>
		/// <param name="β">The scalar to be multiplied by <paramref name="C"/>. If it is 0, the original values of <paramref name="C"/> will be ignored.</param>
		/// <param name="C">The symmetric/hermitian matrix of dimension <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(0, <paramref name="n"/>)</c></param>
		/// <param name="ldc">The leading dimension of two-dimensional array used to store matrix <paramref name="C"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="C"/> is null or invalid</exception>
		protected abstract bool RankTwoKUpdate_<T>(bool fillUpper, MatrixOperation op, bool conjugate, long n, long k, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, perform the variant symmetric/hermitian rank-k update:<br/>
		/// <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="op"/>(<paramref name="A"/>) * <paramref name="op"/>(<paramref name="B"/>)^pow + <paramref name="β"/> * <paramref name="C"/></c>, <c>pow = <paramref name="conjB"/> ? H : T</c>.<br/>
		/// Where <paramref name="α"/> and <paramref name="β"/> are scalars, <paramref name="C"/> is a symmetric/hermitian matrix stored in lower or upper mode, and <paramref name="A"/> and <paramref name="B"/> are matrices with dimensions <c><paramref name="op"/>(<paramref name="A"/>) == <paramref name="op"/>(<paramref name="B"/>) == <paramref name="n"/>×<paramref name="k"/></c>.<br/>
		/// This routine can be used when the matrix <paramref name="B"/> is in such way that the result is guaranteed to be hermitian. For example, <paramref name="B"/> is a column-wise scaling of <paramref name="A"/>.
		/// </summary>
		/// <param name="fillUpper">The <see cref="bool"/> indicates whether matrix <paramref name="C"/>'s upper or lower part will be overwritten</param>
		/// <param name="op">The <see cref="MatrixOperation"/> indicates the simple operation to <paramref name="A"/> and <paramref name="B"/></param>
		/// <param name="conjB">Conjugate transpose <paramref name="B"/> or just transpose <paramref name="B"/></param>
		/// <param name="n">The number of rows of matrix <paramref name="op"/>(<paramref name="A"/>), <paramref name="op"/>(<paramref name="B"/>) and <paramref name="C"/></param>
		/// <param name="k">The number of columns of matrix <paramref name="op"/>(<paramref name="A"/>) and <paramref name="op"/>(<paramref name="B"/>)</param>
		/// <param name="α">The scalar to be multiplied to <paramref name="op"/>(<paramref name="A"/>) * <paramref name="op"/>(<paramref name="A"/>)^pow</param>
		/// <param name="A">The array of column major with leading dimension = <paramref name="lda"/></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/>, must be of at least its number of rows</param>
		/// <param name="B">The array of column major with leading dimension = <paramref name="ldb"/></param>
		/// <param name="ldb">The leading dimension of two-dimensional array used to store matrix <paramref name="B"/>, must be of at least its number of rows</param>
		/// <param name="β">The scalar to be multiplied by <paramref name="C"/>. If it is 0, the original values of <paramref name="C"/> will be ignored.</param>
		/// <param name="C">The symmetric/hermitian matrix of dimension <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(0, <paramref name="n"/>)</c></param>
		/// <param name="ldc">The leading dimension of two-dimensional array used to store matrix <paramref name="C"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="C"/> is null or invalid</exception>
		protected abstract bool RankKUpdateVariant_<T>(bool fillUpper, MatrixOperation op, bool conjB, long n, long k, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, solves the triangular linear systems with multiple right-hand-sides for <c>x</c> and overwrite it to <paramref name="B"/>:<br/>
		/// <c><paramref name="op"/>(<paramref name="A"/>) * x == <paramref name="α"/> * <paramref name="B"/></c> if <paramref name="leftA"/> is true, or <c>x * <paramref name="op"/>(<paramref name="A"/>) == <paramref name="α"/> * <paramref name="B"/></c> otherwise.
		/// </summary>
		/// <param name="leftA">Whether the matrix <paramref name="A"/> is at left side or right side</param>
		/// <param name="fillUpper">Whether the matrix <paramref name="A"/>'s upper or lower triangle is filled</param>
		/// <param name="unitDiag">Whether the matrix <paramref name="A"/>'s diagonal elements are all 1 or not</param>
		/// <param name="op">The <see cref="MatrixOperation"/> indicates the simple operation to <paramref name="A"/></param>
		/// <param name="m">The number of rows and columns of <paramref name="A"/> and number of rows of <paramref name="B"/></param>
		/// <param name="n">The number of columns of <paramref name="B"/>, i.e., the number of linear systems to be solved</param>
		/// <param name="α">The scalar to multiply to <paramref name="B"/></param>
		/// <param name="A">The input triangular matrix <paramref name="A"/> of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(0, <paramref name="m"/>)</c></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="B">The input/output right-hand-side matrix. Overwritten by the solutions at exit.</param>
		/// <param name="ldb">The leading dimension of two-dimensional array used to store matrix <paramref name="B"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> is null or invalid</exception>
		protected abstract bool TriangularMatrixSolve_<T>(bool leftA, bool fillUpper, bool unitDiag, MatrixOperation op, long m, long n, T α, Storage<T> A, long lda, Storage<T> B, long ldb) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, multiply the triangular matrix <paramref name="A"/> with the given matrix <paramref name="B"/> and overwrite the result to <paramref name="C"/>:<br/>
		/// <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="op"/>(<paramref name="A"/>) * <paramref name="B"/></c> if <paramref name="leftA"/> is true, or <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="B"/> * <paramref name="op"/>(<paramref name="A"/>)</c> otherwise.
		/// </summary>
		/// <param name="leftA">Whether the matrix <paramref name="A"/> is at left side or right side</param>
		/// <param name="fillUpper">Whether the matrix <paramref name="A"/>'s upper or lower triangle is filled</param>
		/// <param name="unitDiag">Whether the matrix <paramref name="A"/>'s diagonal elements are all 1 or not</param>
		/// <param name="op">The <see cref="MatrixOperation"/> indicates the simple operation to <paramref name="A"/></param>
		/// <param name="m">The number of rows of <paramref name="B"/></param>
		/// <param name="n">The number of columns of <paramref name="B"/></param>
		/// <param name="α">The scalar to multiply to <paramref name="B"/></param>
		/// <param name="A">The input triangular matrix <paramref name="A"/> of dimension <c><paramref name="lda"/>×<paramref name="m"/></c> if <paramref name="leftA"/>, or <c><paramref name="lda"/>×<paramref name="n"/></c> otherwise</param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="B">The input general matrix of dimension <c><paramref name="ldb"/>×<paramref name="n"/></c></param>
		/// <param name="ldb">The leading dimension of two-dimensional array used to store matrix <paramref name="B"/></param>
		/// <param name="C">The output matrix to be overwritten by the result at exit, can be <paramref name="B"/> when <paramref name="ldc"/> == <paramref name="ldb"/>.</param>
		/// <param name="ldc">The leading dimension of two-dimensional array used to store matrix <paramref name="C"/>, must be <paramref name="ldb"/> when <paramref name="B"/> == <paramref name="C"/>.</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		protected abstract bool TriangularMatrixMultiply_<T>(bool leftA, bool fillUpper, bool unitDiag, MatrixOperation op, long m, long n, T α, Storage<T> A, long lda, Storage<T> B, long ldb, Storage<T> C, long ldc) where T : unmanaged, INumber<T>;
		#endregion
		#endregion
	}
}