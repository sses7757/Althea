using System;
using System.Collections.Generic;


namespace Althea.LinearAlgebra.Dense
{
	/// <summary>
	/// The abstract class for runtime dense linear algebra API routines 
	/// </summary>
	public abstract partial class AbstractApi : AbstractRuntimeApi
	{
		#region support information
		/// <summary>
		/// When implemented by a derived class, check if the given <paramref name="location"/> is supported by matrix unary operations of this implementation or not.
		/// </summary>
		/// <param name="location">The given <see cref="CombinationOfLocations"/></param>
		/// <returns>Whether matrix unary operation on <paramref name="location"/> is supported by this <see cref="AbstractApi"/>.</returns>
		/// <remarks>
		/// The unary operations:
		/// <list type="bullet">
		/// <item>etc.</item>
		/// </list>
		/// </remarks>
		protected abstract bool IsSupportedMatrixUnary(CombinationOfLocations location);

		/// <summary>
		/// When implemented by a derived class, check if the given <see cref="CombinationOfLocations"/>s are supported by unary vector and binary matrix operations of this implementation or not.
		/// </summary>
		/// <param name="vector">The given <see cref="CombinationOfLocations"/> of the vector</param>
		/// <param name="matrix1">The given <see cref="CombinationOfLocations"/> of the first matrix</param>
		/// <param name="matrix2">The given <see cref="CombinationOfLocations"/> of the second matrix</param>
		/// <returns>Whether unary vector and binary matrix operations between <paramref name="vector"/> and <paramref name="matrix1"/> and <paramref name="matrix2"/> are supported by this <see cref="AbstractApi"/>.</returns>
		/// <remarks>
		/// The binary operations:
		/// <list type="bullet">
		/// <item>etc.</item>
		/// </list>
		/// </remarks>
		protected abstract bool IsSupportedVectorUnaryMatrixUBinary(CombinationOfLocations vector, CombinationOfLocations matrix1, CombinationOfLocations matrix2);
		#endregion


		#region static methods as dispatchers
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
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentException">If the parameters do not fit any mode</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> and <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		public static void GeneralMatricesAdd<T>(MatrixOperation opA, MatrixOperation opB, long m, long n, T α, Storage<T>? A, long lda, T β, Storage<T>? B, long ldb, Storage<T> C, long ldc) where T : unmanaged, IEquatable<T>
		{
			if (A is null && B is null)
				throw new ArgumentNullException($"{nameof(A)}, {nameof(B)}");
			if (C is null)
				throw new ArgumentNullException(nameof(C));
			Predicate<AbstractApi> predicate;
			if (A is null && B is not null)
			{
				CombinationOfLocations location1 = B.LocationDescription, location3 = C.LocationDescription;
				predicate = a => a.IsSupportedMatrixBinary(location1, location3);
			}
			else if (A is not null && B is null)
			{
				CombinationOfLocations location1 = A.LocationDescription, location3 = C.LocationDescription;
				predicate = a => a.IsSupportedMatrixBinary(location1, location3);
			}
			else
			{
#pragma warning disable CS8602 // A and B are not null here
				CombinationOfLocations location1 = A.LocationDescription, location2 = B.LocationDescription, location3 = C.LocationDescription;
#pragma warning restore CS8602
				predicate = a => a.IsSupportedMatrixTrinary(location1, location2, location3);
			}
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, predicate, node);
				success = node.Value.GeneralMatricesAdd_(opA, opB, m, n, α, A, lda, β, B, ldb, C, ldc);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// When implemented by a derived class, perform the matrix-matrix multiplication:
		/// <list type="table">
		/// <listheader><term>Condition</term>  <description>Equation</description></listheader>
		/// <item><term><paramref name="leftA"/> is true</term>  <description><paramref name="C"/> = <paramref name="α"/> * <paramref name="A"/> * diag(<paramref name="x"/>) + <paramref name="β"/> * <paramref name="C"/></description></item>
		/// <item><term><paramref name="leftA"/> is false</term>  <description><paramref name="C"/> = <paramref name="α"/> * diag(<paramref name="x"/>) * <paramref name="A"/> + <paramref name="β"/> * <paramref name="C"/></description></item>
		/// </list>
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
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
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="A"/> or <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> ≤ 0</exception>
		public static void DiagonalMatrixMultiplyGeneral<T>(bool leftA, long m, long n, T α, Storage<T> A, long lda, Storage<T> x, int strideX, T β, Storage<T> C, long ldc) where T : unmanaged, IEquatable<T>
		{
			CombinationOfLocations location1 = A.LocationDescription, location2 = C.LocationDescription, locationVec = x.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedVectorUnaryMatrixUBinary(locationVec, location1, location2), node);
				success = node.Value.DiagonalMatrixMultiplyGeneral_(leftA, m, n, α, A, lda, x, strideX, β, C, ldc);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}
		#endregion

		#region custom BLAS level 1
		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="x"/> = <paramref name="x"/>.^<paramref name="p"/></c> (point-wise power).
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be powered in-place</param>
		/// <param name="stride">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="p">The exponent as a <see cref="double"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="stride"/> is less than 1</exception>
		public static void PointWisePower<T>(Storage<T> x, int stride, double p) where T : unmanaged
		{
			CombinationOfLocations location1 = x.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedVectorUnary(location1), node);
				success = node.Value.PointWisePower_(x, stride, p);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="x"/> = <paramref name="x"/>.^<paramref name="p"/></c> (point-wise power).
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be powered in-place</param>
		/// <param name="stride">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="p">The exponent as a <typeparamref name="T"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="stride"/> is less than 1</exception>
		public static void PointWisePower<T>(Storage<T> x, int stride, T p) where T : unmanaged, IEquatable<T>
		{
			CombinationOfLocations location1 = x.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedVectorUnary(location1), node);
				success = node.Value.PointWisePower_(x, stride, p);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="x"/> = conj(<paramref name="x"/>)</c> (point-wise conjugate).
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be conjugated</param>
		/// <param name="stride">The stride between consecutive elements of <paramref name="x"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="stride"/> is less than 1</exception>
		public static void PointWiseConjugate<T>(Storage<T> x, int stride) where T : unmanaged
		{
			CombinationOfLocations location1 = x.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedVectorUnary(location1), node);
				success = node.Value.PointWiseConjugate_(x, stride);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// When implemented by a derived class, truncate the vector by comparing each element's absolute value in <paramref name="x"/> to the given <paramref name="threshold"/>, if it is smaller than <paramref name="threshold"/>, it will be set to 0.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be truncated</param>
		/// <param name="threshold">If any element's absolute value is smaller than <paramref name="threshold"/>, it will be set to 0</param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="x"/> is null or invalid</exception>
		public static void TruncateArray<T>(Storage<T> x, double threshold) where T : unmanaged
		{
			CombinationOfLocations location1 = x.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedVectorUnary(location1), node);
				success = node.Value.TruncateArray_(x, threshold);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// When implemented by a derived class, aggregately sum the elements in vector <paramref name="x"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be summed</param>
		/// <param name="stride">The stride between consecutive elements of <paramref name="x"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="stride"/> is less than 1</exception>
		/// <returns>The sum as a <typeparamref name="T"/></returns>
		public static T AggregateSum<T>(Storage<T> x, int stride) where T : unmanaged
		{
			CombinationOfLocations location1 = x.LocationDescription;
			T result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedVectorUnary(location1), node);
				success = node.Value.AggregateSum_(x, stride, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// When implemented by a derived class, aggregately product the elements in vector <paramref name="x"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be multiplied</param>
		/// <param name="stride">The stride between consecutive elements of <paramref name="x"/></param>
		/// <returns>The sum as a <typeparamref name="T"/></returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="stride"/> is less than 1</exception>
		public static T AggregateProduct<T>(Storage<T> x, int stride) where T : unmanaged
		{
			CombinationOfLocations location1 = x.LocationDescription;
			T result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedVectorUnary(location1), node);
				success = node.Value.AggregateProduct_(x, stride, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="x"/> = <paramref name="x"/> + <paramref name="α"/></c> (point-wise addition).
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be added in-place</param>
		/// <param name="stride">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="α">The scalar to add</param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="stride"/> is less than 1</exception>
		public static void PointWiseAddScalar<T>(Storage<T> x, int stride, T α) where T : unmanaged, IEquatable<T>
		{
			CombinationOfLocations location1 = x.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedVectorUnary(location1), node);
				success = node.Value.PointWiseAddScalar_(x, stride, α);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// When implemented by a derived class, check if all elements in <paramref name="x"/> and <paramref name="y"/> are equal: <c><paramref name="x"/>[i] == <paramref name="y"/>[j]</c> (point-wise equals).
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be checked</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The other vector to be checked</param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="x"/></param>
		/// <returns>Whether all elements in <paramref name="x"/> and <paramref name="y"/> are equal</returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> is less than 1</exception>
		public static bool PointWiseEquals<T>(Storage<T> x, int strideX, Storage<T> y, int strideY) where T : unmanaged
		{
			CombinationOfLocations location1 = x.LocationDescription, location2 = y.LocationDescription;
			bool result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedVectorBinary(location1, location2), node);
				success = node.Value.PointWiseEquals_(x, strideX, y, strideY, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="x"/> = <paramref name="x"/>.*<paramref name="y"/></c> (point-wise multiplication).
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be multiplied in-place</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The vector to multiply</param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="x"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> is less than 1</exception>
		public static void PointWiseMultiply<T>(Storage<T> x, int strideX, Storage<T> y, int strideY) where T : unmanaged
		{
			CombinationOfLocations location1 = x.LocationDescription, location2 = y.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedVectorBinary(location1, location2), node);
				success = node.Value.PointWiseMultiply_(x, strideX, y, strideY);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="x"/> = <paramref name="x"/>./<paramref name="y"/></c> (point-wise division).
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be multiplied in-place</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The vector to multiply</param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="x"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> is less than 1</exception>
		public static void PointWiseDivide<T>(Storage<T> x, int strideX, Storage<T> y, int strideY) where T : unmanaged
		{
			CombinationOfLocations location1 = x.LocationDescription, location2 = y.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedVectorBinary(location1, location2), node);
				success = node.Value.PointWiseDivide_(x, strideX, y, strideY);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// When implemented by a derived class, cast the given vector from type <typeparamref name="T"/> to type <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the input data type</typeparam>
		/// <typeparam name="TOut">Any unmanaged struct as the output data type</typeparam>
		/// <param name="source">The source vector</param>
		/// <param name="strideSrc">The stride between consecutive elements of <paramref name="source"/></param>
		/// <param name="destination">The destination vector</param>
		/// <param name="strideDst">The stride between consecutive elements of <paramref name="destination"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="source"/> or <paramref name="destination"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideSrc"/> or <paramref name="strideDst"/> is less than 1</exception>
		public static void PointWiseCast<T, TOut>(Storage<T> source, int strideSrc, Storage<TOut> destination, int strideDst) where T : unmanaged where TOut : unmanaged
		{
			CombinationOfLocations location1 = source.LocationDescription, location2 = destination.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedVectorBinary(location1, location2), node);
				success = node.Value.PointWiseCast_(source, strideSrc, destination, strideDst);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// When implemented by a derived class, preform partial aggregate sum of the elements in vector <paramref name="x"/> and write the result to <paramref name="y"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be partially summed</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The vector to store the partial sum result</param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="y"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> is less than 1</exception>
		public static void PartialSum<T>(Storage<T> x, int strideX, Storage<T> y, int strideY) where T : unmanaged
		{
			CombinationOfLocations location1 = x.LocationDescription, location2 = y.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedVectorBinary(location1, location2), node);
				success = node.Value.PartialSum_(x, strideX, y, strideY);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// When implemented by a derived class, preform partial aggregate product of the elements in vector <paramref name="x"/> and write the result to <paramref name="y"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be partially multiplied</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The vector to store the partial product result</param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="y"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> is less than 1</exception>
		public static void PartialProduct<T>(Storage<T> x, int strideX, Storage<T> y, int strideY) where T : unmanaged
		{
			CombinationOfLocations location1 = x.LocationDescription, location2 = y.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedVectorBinary(location1, location2), node);
				success = node.Value.PartialProduct_(x, strideX, y, strideY);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}
		#endregion

		#region custom BLAS level 3
		/// <summary>
		/// When implemented by a derived class, copy the matrix <paramref name="A"/>'s upper part to lower part and set the diagonal elements to its absolute value is <typeparamref name="T"/> is a complex type.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="A">The matrix with size <paramref name="n"/>×<paramref name="n"/></param>
		/// <param name="ld">The leading dimension of <paramref name="A"/>, must be at least <paramref name="n"/></param>
		/// <param name="n">The number of rows and columns of <paramref name="A"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="A"/> is null or invalid</exception>
		public static void MatrixCopyUpperToLowerPart<T>(Storage<T> A, long ld, long n) where T : unmanaged
		{
			CombinationOfLocations location1 = A.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedMatrixUnary(location1), node);
				success = node.Value.MatrixCopyUpperToLowerPart_(A, ld, n);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// When implemented by a derived class, calculate matrix Kronecker product:<br/>
		/// <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="A"/> ⊗ <paramref name="B"/> + <paramref name="β"/> * <paramref name="C"/></c>.
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
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="A"/> or <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		public static void MatrixKronecker<T>(long ma, long na, long mb, long nb, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc) where T : unmanaged, IEquatable<T>
		{
			CombinationOfLocations location1 = A.LocationDescription, location2 = B.LocationDescription, location3 = C.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedMatrixTrinary(location1, location2, location3), node);
				success = node.Value.MatrixKronecker_(ma, na, mb, nb, α, A, lda, B, ldb, β, C, ldc);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}
		#endregion
		#endregion


		#region abstract methods that actually do computations
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
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentException">If the parameters do not fit any mode</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> and <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		protected abstract bool GeneralMatricesAdd_<T>(MatrixOperation opA, MatrixOperation opB, long m, long n, T α, Storage<T>? A, long lda, T β, Storage<T>? B, long ldb, Storage<T> C, long ldc) where T : unmanaged, IEquatable<T>;

		/// <summary>
		/// When implemented by a derived class, perform the matrix-matrix multiplication:
		/// <list type="table">
		/// <listheader><term>Condition</term>  <description>Equation</description></listheader>
		/// <item><term><paramref name="leftA"/> is true</term>  <description><paramref name="C"/> = <paramref name="α"/> * <paramref name="A"/> * diag(<paramref name="x"/>) + <paramref name="β"/> * <paramref name="C"/></description></item>
		/// <item><term><paramref name="leftA"/> is false</term>  <description><paramref name="C"/> = <paramref name="α"/> * diag(<paramref name="x"/>) * <paramref name="A"/> + <paramref name="β"/> * <paramref name="C"/></description></item>
		/// </list>
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
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
		protected abstract bool DiagonalMatrixMultiplyGeneral_<T>(bool leftA, long m, long n, T α, Storage<T> A, long lda, Storage<T> x, int strideX, T β, Storage<T> C, long ldc) where T : unmanaged, IEquatable<T>;
		#endregion

		#region custom BLAS level 1
		/// <summary>
		/// When implemented by a derived class, check if all elements in <paramref name="x"/> and <paramref name="y"/> are equal: <c><paramref name="x"/>[i] == <paramref name="y"/>[j]</c> (point-wise equals).
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be checked</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The other vector to be checked</param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="equals">Output <see cref="bool"/> indicating whether all elements in <paramref name="x"/> and <paramref name="y"/> are equal</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> is less than 1</exception>
		protected abstract bool PointWiseEquals_<T>(Storage<T> x, int strideX, Storage<T> y, int strideY, out bool equals) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="x"/> = <paramref name="x"/>.*<paramref name="y"/></c> (point-wise multiplication).
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be multiplied in-place</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The vector to multiply</param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="x"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> is less than 1</exception>
		protected abstract bool PointWiseMultiply_<T>(Storage<T> x, int strideX, Storage<T> y, int strideY) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="x"/> = <paramref name="x"/>./<paramref name="y"/></c> (point-wise division).
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be multiplied in-place</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The vector to multiply</param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="x"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> is less than 1</exception>
		protected abstract bool PointWiseDivide_<T>(Storage<T> x, int strideX, Storage<T> y, int strideY) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="x"/> = <paramref name="x"/>.^<paramref name="p"/></c> (point-wise power).
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be powered in-place</param>
		/// <param name="stride">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="p">The exponent as a <see cref="double"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="stride"/> is less than 1</exception>
		protected abstract bool PointWisePower_<T>(Storage<T> x, int stride, double p) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="x"/> = <paramref name="x"/>.^<paramref name="p"/></c> (point-wise power).
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be powered in-place</param>
		/// <param name="stride">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="p">The exponent as a <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="stride"/> is less than 1</exception>
		protected abstract bool PointWisePower_<T>(Storage<T> x, int stride, T p) where T : unmanaged, IEquatable<T>;

		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="x"/> = conj(<paramref name="x"/>)</c> (point-wise conjugate).
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be conjugated</param>
		/// <param name="stride">The stride between consecutive elements of <paramref name="x"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="stride"/> is less than 1</exception>
		protected abstract bool PointWiseConjugate_<T>(Storage<T> x, int stride) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, cast the given vector from type <typeparamref name="T"/> to type <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the input data type</typeparam>
		/// <typeparam name="TOut">Any unmanaged struct as the output data type</typeparam>
		/// <param name="source">The source vector</param>
		/// <param name="incSrc">The stride between consecutive elements of <paramref name="source"/></param>
		/// <param name="destination">The destination vector</param>
		/// <param name="incDst">The stride between consecutive elements of <paramref name="destination"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="incSrc"/> or <paramref name="incDst"/> is less than 1</exception>
		protected abstract bool PointWiseCast_<T, TOut>(Storage<T> source, int incSrc, Storage<TOut> destination, int incDst) where T : unmanaged where TOut : unmanaged;

		/// <summary>
		/// When implemented by a derived class, truncate the vector by comparing each element's absolute value in <paramref name="x"/> to the given <paramref name="threshold"/>, if it is smaller than <paramref name="threshold"/>, it will be set to 0.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be truncated</param>
		/// <param name="threshold">If any element's absolute value is smaller than <paramref name="threshold"/>, it will be set to 0</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		protected abstract bool TruncateArray_<T>(Storage<T> x, double threshold) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, aggregately sum the elements in vector <paramref name="x"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be summed</param>
		/// <param name="stride">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="sum">Output the sum as a <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="stride"/> is less than 1</exception>
		protected abstract bool AggregateSum_<T>(Storage<T> x, int stride, out T sum) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, aggregately product the elements in vector <paramref name="x"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be multiplied</param>
		/// <param name="stride">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="product">Output the product as a <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="stride"/> is less than 1</exception>
		protected abstract bool AggregateProduct_<T>(Storage<T> x, int stride, out T product) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, preform partial aggregate sum of the elements in vector <paramref name="x"/> and write the result to <paramref name="y"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be partially summed</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The vector to store the partial sum result</param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="y"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> is less than 1</exception>
		protected abstract bool PartialSum_<T>(Storage<T> x, int strideX, Storage<T> y, int strideY) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, preform partial aggregate product of the elements in vector <paramref name="x"/> and write the result to <paramref name="y"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be partially multiplied</param>
		/// <param name="strideX">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The vector to store the partial product result</param>
		/// <param name="strideY">The stride between consecutive elements of <paramref name="y"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideX"/> or <paramref name="strideY"/> is less than 1</exception>
		protected abstract bool PartialProduct_<T>(Storage<T> x, int strideX, Storage<T> y, int strideY) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, compute <c><paramref name="x"/> = <paramref name="x"/> + <paramref name="α"/></c> (point-wise addition).
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector to be added in-place</param>
		/// <param name="stride">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="α">The scalar to add</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="stride"/> is less than 1</exception>
		protected abstract bool PointWiseAddScalar_<T>(Storage<T> x, int stride, T α) where T : unmanaged, IEquatable<T>;
		#endregion

		#region custom BLAS level 3
		/// <summary>
		/// When implemented by a derived class, copy the matrix <paramref name="A"/>'s upper part to lower part and set the diagonal elements to its absolute value is <typeparamref name="T"/> is a complex type.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="A">The matrix with size <paramref name="n"/>×<paramref name="n"/></param>
		/// <param name="ld">The leading dimension of <paramref name="A"/>, must be at least <paramref name="n"/></param>
		/// <param name="n">The number of rows and columns of <paramref name="A"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> is null or invalid</exception>
		protected abstract bool MatrixCopyUpperToLowerPart_<T>(Storage<T> A, long ld, long n) where T : unmanaged;

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
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		protected abstract bool MatrixKronecker_<T>(long ma, long na, long mb, long nb, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc) where T : unmanaged, IEquatable<T>;
		#endregion
		#endregion
	}
}
