using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.Linq;


namespace Althea.LinearAlgebra.Dense
{
	/// <summary>
	/// The abstract class for runtime dense linear algebra API routines 
	/// </summary>
	public abstract partial class AbstractApi : AbstractRuntimeApi
	{
		#region static methods for dispatching
		/// <summary>
		/// Get the current using <see cref="AbstractApi"/>.
		/// </summary>
		/// <remarks><b>DO NOT</b> invoke methods of this property directly unless you are sure about what you are doing; otherwise, there may be exceptions and / or unnoticeable bugs.</remarks>
		public static AbstractApi? Current => RecentAPIs.First?.Value;

		private static readonly LinkedList<AbstractApi> RecentAPIs = new LinkedList<AbstractApi>();

		internal static bool SetImplementation(Type implementation) => SetImplementation(RecentAPIs, implementation);

		/// <summary>
		/// Special version for <see cref="AbstractApi"/> of method <see cref="AbstractRuntimeApi.DisposeNotCurrent{T}(LinkedList{T})"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void DisposeNotCurrent() => DisposeNotCurrent(RecentAPIs);

		/// <summary>
		/// Special version for <see cref="AbstractApi"/> of method <see cref="AbstractRuntimeApi.SelectImplementation{T}(LinkedList{T}, IStorage)"/>
		/// </summary>
		public static AbstractApi SelectImplementation<T>(Storage<T> storage) where T : unmanaged => SelectImplementation(RecentAPIs, storage);

		/// <summary>
		/// Special version for <see cref="AbstractApi"/> of method <see cref="AbstractRuntimeApi.SelectImplementation{T}(LinkedList{T}, IStorage)"/>
		/// </summary>
		public static AbstractApi SelectImplementation<T, TOther>(Storage<T> storage, Storage<TOther> storageOther) where T : unmanaged where
			TOther : unmanaged
		{
			var selectOther = SelectImplementation(RecentAPIs, storageOther);
			var selectThis = SelectImplementation(RecentAPIs, storage);
			if (ReferenceEquals(selectThis, selectOther))
				return selectThis;
			else
				throw new InvalidOperationException(Resources.Backend.NotAvailable);
		}

		/// <summary>
		/// Special version for <see cref="AbstractApi"/> of method <see cref="AbstractRuntimeApi.SelectImplementation{T}(LinkedList{T}, IStorage)"/>
		/// </summary>
		public static AbstractApi SelectImplementation<T, TOther>(Storage<T> storage, Storage<TOther> storageOther1, Storage<TOther> storageOther2) where T : unmanaged where
			TOther : unmanaged
		{
			var selectOther = SelectImplementation(RecentAPIs, storageOther1, storageOther2);
			var selectThis = SelectImplementation(RecentAPIs, storage);
			if (ReferenceEquals(selectThis, selectOther))
				return selectThis;
			else
				throw new InvalidOperationException(Resources.Backend.NotAvailable);
		}

		/// <summary>
		/// Special version for <see cref="AbstractApi"/> of method <see cref="AbstractRuntimeApi.SelectImplementation{T}(LinkedList{T}, IStorage)"/>
		/// </summary>
		public static AbstractApi SelectImplementation<T, TOther>(Storage<T> storage, Storage<TOther> storageOther1, Storage<TOther> storageOther2, Storage<TOther> storageOther3) where T : unmanaged where
			TOther : unmanaged
		{
			var selectOther = SelectImplementation(RecentAPIs, storageOther1, storageOther2, storageOther3);
			var selectThis = SelectImplementation(RecentAPIs, storage);
			if (ReferenceEquals(selectThis, selectOther))
				return selectThis;
			else
				throw new InvalidOperationException(Resources.Backend.NotAvailable);
		}

		/// <summary>
		/// Special version for <see cref="AbstractApi"/> of method <see cref="AbstractRuntimeApi.SelectImplementation{T}(LinkedList{T}, IStorage, IStorage)"/>
		/// </summary>
		public static AbstractApi SelectImplementation<T>(Storage<T> storage1, Storage<T> storage2) where T : unmanaged => SelectImplementation(RecentAPIs, storage1, storage2);

		/// <summary>
		/// Special version for <see cref="AbstractApi"/> of method <see cref="AbstractRuntimeApi.SelectImplementation{T}(LinkedList{T}, IStorage, IStorage)"/>
		/// </summary>
		public static AbstractApi SelectImplementation<T, TOther>(Storage<T> storage1, Storage<T> storage2, Storage<TOther> storageOther) where T : unmanaged where
			TOther : unmanaged
		{
			var selectOther = SelectImplementation(RecentAPIs, storageOther);
			var selectThis = SelectImplementation(RecentAPIs, storage1, storage2);
			if (ReferenceEquals(selectThis, selectOther))
				return selectThis;
			else
				throw new InvalidOperationException(Resources.Backend.NotAvailable);
		}

		/// <summary>
		/// Special version for <see cref="AbstractApi"/> of method <see cref="AbstractRuntimeApi.SelectImplementation{T}(LinkedList{T}, IStorage, IStorage)"/>
		/// </summary>
		public static AbstractApi SelectImplementation<T, TOther>(Storage<T> storage1, Storage<T> storage2, Storage<TOther> storageOther1, Storage<TOther> storageOther2) where T : unmanaged where
			TOther : unmanaged
		{
			var selectOther = SelectImplementation(RecentAPIs, storageOther1, storageOther2);
			var selectThis = SelectImplementation(RecentAPIs, storage1, storage2);
			if (ReferenceEquals(selectThis, selectOther))
				return selectThis;
			else
				throw new InvalidOperationException(Resources.Backend.NotAvailable);
		}

		/// <summary>
		/// Special version for <see cref="AbstractApi"/> of method <see cref="AbstractRuntimeApi.SelectImplementation{T}(LinkedList{T}, IStorage, IStorage)"/>
		/// </summary>
		public static AbstractApi SelectImplementation<T, TOther>(Storage<T> storage1, Storage<T> storage2, Storage<TOther> storageOther1, Storage<TOther> storageOther2, Storage<TOther> storageOther3) where T : unmanaged where
			TOther : unmanaged
		{
			var selectOther = SelectImplementation(RecentAPIs, storageOther1, storageOther2, storageOther3);
			var selectThis = SelectImplementation(RecentAPIs, storage1, storage2);
			if (ReferenceEquals(selectThis, selectOther))
				return selectThis;
			else
				throw new InvalidOperationException(Resources.Backend.NotAvailable);
		}

		/// <summary>
		/// Special version for <see cref="AbstractApi"/> of method <see cref="AbstractRuntimeApi.SelectImplementation{T}(LinkedList{T}, IStorage, IStorage, IStorage)"/>
		/// </summary>
		public static AbstractApi SelectImplementation<T>(Storage<T> storage1, Storage<T> storage2, Storage<T> storage3) where T : unmanaged => SelectImplementation(RecentAPIs, storage1, storage2, storage3);

		/// <summary>
		/// Special version for <see cref="AbstractApi"/> of method <see cref="AbstractRuntimeApi.SelectImplementation{T}(LinkedList{T}, IStorage, IStorage, IStorage)"/>
		/// </summary>
		public static AbstractApi SelectImplementation<T, TOther>(Storage<T> storage1, Storage<T> storage2, Storage<T> storage3, Storage<TOther> storageOther) where T : unmanaged where
			TOther : unmanaged
		{
			var selectOther = SelectImplementation(RecentAPIs, storageOther);
			var selectThis = SelectImplementation(RecentAPIs, storage1, storage2, storage3);
			if (ReferenceEquals(selectThis, selectOther))
				return selectThis;
			else
				throw new InvalidOperationException(Resources.Backend.NotAvailable);
		}

		/// <summary>
		/// Special version for <see cref="AbstractApi"/> of method <see cref="AbstractRuntimeApi.SelectImplementation{T}(LinkedList{T}, IStorage, IStorage, IStorage)"/>
		/// </summary>
		public static AbstractApi SelectImplementation<T, TOther>(Storage<T> storage1, Storage<T> storage2, Storage<T> storage3, Storage<TOther> storageOther1, Storage<TOther> storageOther2) where T : unmanaged where
			TOther : unmanaged
		{
			var selectOther = SelectImplementation(RecentAPIs, storageOther1, storageOther2);
			var selectThis = SelectImplementation(RecentAPIs, storage1, storage2, storage3);
			if (ReferenceEquals(selectThis, selectOther))
				return selectThis;
			else
				throw new InvalidOperationException(Resources.Backend.NotAvailable);
		}

		/// <summary>
		/// Special version for <see cref="AbstractApi"/> of method <see cref="AbstractRuntimeApi.SelectImplementation{T}(LinkedList{T}, IStorage, IStorage, IStorage)"/>
		/// </summary>
		public static AbstractApi SelectImplementation<T, TOther>(Storage<T> storage1, Storage<T> storage2, Storage<T> storage3, Storage<TOther> storageOther1, Storage<TOther> storageOther2, Storage<TOther> storageOther3) where T : unmanaged where
			TOther : unmanaged
		{
			var selectOther = SelectImplementation(RecentAPIs, storageOther1, storageOther2, storageOther3);
			var selectThis = SelectImplementation(RecentAPIs, storage1, storage2, storage3);
			if (ReferenceEquals(selectThis, selectOther))
				return selectThis;
			else
				throw new InvalidOperationException(Resources.Backend.NotAvailable);
		}
		#endregion


		#region support information
		// Ignore Spelling: N-ary
		/// <summary>
		/// Get list of the supported <see cref="CombinationOfLocations"/> for all N-ary operations. Each value in the list is a set of <paramref name="N"/> values to indicate a supported combination of certain <see cref="CombinationOfLocations"/>. Or null if there are no N-ary operations.
		/// </summary>
		/// <param name="N">The number of operands, must be <paramref name="N"/> &gt; 0</param>
		/// <returns>The list of the supported memory locations for all N-ary operations. Or null if there are no N-ary operations.</returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="N"/> &lt;= 0</exception>
		public override IReadOnlyList<IImmutableSet<CombinationOfLocations>> SupportedNaryLocations(int N)
		{
			return N switch
			{
				1 => this.SupportedUnaryLocations.Select(static l => (IImmutableSet<CombinationOfLocations>)(ImmutableZeroOneElementSet<CombinationOfLocations>)l),
				2 => this.SupportedBinaryLocations.Select(static l => (IImmutableSet<CombinationOfLocations>)l),
				3 => this.SupportedTernaryLocations.Select(static l => (IImmutableSet<CombinationOfLocations>)l),
				> 3 => Array.Empty<IImmutableSet<CombinationOfLocations>>(), // there are no N-ary operations
				_ => throw new ArgumentOutOfRangeException(nameof(N)),
			};
		}

		/// <summary>
		/// When implemented by a derived class, get the list of supported transfer between <see cref="CombinationOfLocations"/> and C# managed memory
		/// </summary>
		public abstract IReadOnlyList<CombinationOfLocations> SupportedManagedTransfer { get; }

		/// <summary>
		/// When implemented by a derived class, check whether the given <see cref="CombinationOfLocations"/> can transfer data with C# managed memory using this implementation
		/// </summary>
		/// <param name="locations">The <see cref="CombinationOfLocations"/> to indicate the unmanaged storage location combination</param>
		/// <returns>Whether this implementation supports data transfer between <paramref name="locations"/> and C# managed memory</returns>
		public virtual bool IsSupportedTransfer(CombinationOfLocations locations) => this.SupportedManagedTransfer.Contains(locations);
		#endregion


		#region BLAS level 1
		/// <summary>
		/// When implemented by a derived class, find the (smallest) index of the element with the maximum magnitude.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector of type <typeparamref name="T"/></param>
		/// <param name="incx">The stride between consecutive elements of <paramref name="x"/></param>
		/// <returns>The resulting index</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="incx"/> is less than 1</exception>
		public abstract long AbsoluteValueArgMax<T>(Storage<T> x, int incx) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, find the (smallest) index of the element with the minimum magnitude.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector of type <typeparamref name="T"/></param>
		/// <param name="incx">The stride between consecutive elements of <paramref name="x"/></param>
		/// <returns>The resulting index or 0 if <paramref name="incx"/> is less than 1</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="incx"/> is less than 1</exception>
		public abstract long AbsoluteValueArgMin<T>(Storage<T> x, int incx) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, compute the sum of the absolute values of the elements of vector <paramref name="x"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector of type <typeparamref name="T"/></param>
		/// <param name="incx">The stride between consecutive elements of <paramref name="x"/></param>
		/// <returns>The result value as a <see cref="double"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="incx"/> is less than 1</exception>
		public abstract double AbsoluteValueSum<T>(Storage<T> x, int incx) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, multiply the vector <paramref name="x"/> by the scalar <paramref name="α"/> and in-place add it to the vector <paramref name="y"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="α">The scalar used for multiplication</param>
		/// <param name="x">The vector of type <typeparamref name="T"/></param>
		/// <param name="incx">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The another vector of type <typeparamref name="T"/></param>
		/// <param name="incy">The stride between consecutive elements of <paramref name="y"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="incx"/> or <paramref name="incy"/> is less than 1</exception>
		public abstract void VectorGeneralAdd<T>(T α, Storage<T> x, int incx, Storage<T> y, int incy) where T : unmanaged, IEquatable<T>;

		/// <summary>
		/// When implemented by a derived class, compute the dot (inner) product of vectors <paramref name="x"/> and <paramref name="y"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="conjX">Conjugate <paramref name="x"/> or not</param>
		/// <param name="x">The vector of type <typeparamref name="T"/></param>
		/// <param name="incx">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The another vector of type <typeparamref name="T"/></param>
		/// <param name="incy">The stride between consecutive elements of <paramref name="y"/></param>
		/// <returns>The result value as a <typeparamref name="T"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="incx"/> or <paramref name="incy"/> is less than 1</exception>
		public abstract T Dot<T>(bool conjX, Storage<T> x, int incx, Storage<T> y, int incy) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, compute the Euclidean norm (2-norm) of the vector <paramref name="x"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="x">The vector of type <typeparamref name="T"/></param>
		/// <param name="incx">The stride between consecutive elements of <paramref name="x"/></param>
		/// <returns>The result value as a <see cref="double"/>, or 0 if <paramref name="incx"/> ≤ 0</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="incx"/> is less than 1</exception>
		public abstract double Norm<T>(Storage<T> x, int incx) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, in-place scale the vector <paramref name="x"/> by scalar <paramref name="α"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="n">The number of elements in the vector <paramref name="x"/></param>
		/// <param name="α">The scalar used for multiplication</param>
		/// <param name="x">The vector with <paramref name="n"/> elements</param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> is null or invalid</exception>
		/// <param name="incx">The stride between consecutive elements of <paramref name="x"/></param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="incx"/> is less than 1</exception>
		public abstract void Scale<T>(int n, T α, Storage<T> x, int incx) where T : unmanaged, IEquatable<T>;
		#endregion

		#region BLAS level 2
		/// <summary>
		/// When implemented by a derived class, perform the matrix-vector multiplication: <paramref name="y"/> = <paramref name="α"/> * <paramref name="op"/>(<paramref name="A"/>)* <paramref name="x"/> + <paramref name="β"/> * <paramref name="y"/>.<br/>
		/// Where <paramref name="A"/> is a <paramref name="m"/>×<paramref name="n"/> matrix stored in column-major format, <paramref name="x"/> and <paramref name="y"/> are vectors, and <paramref name="α"/> and <paramref name="β"/> are scalars.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="op">The <see cref="MatrixOperation"/> that is non- or (conj.) transpose</param>
		/// <param name="m">The number of rows of matrix <paramref name="A"/></param>
		/// <param name="n">The number of columns of matrix <paramref name="A"/></param>
		/// <param name="α">The scalar to be multiplied to <paramref name="A"/></param>
		/// <param name="A">The input array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="lda">The leading dimension of the two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="x">The vector of length at least <c>(1+(<paramref name="n"/>-1)*<paramref name="incx"/>)</c> elements if <paramref name="op"/>==<see cref="MatrixOperation.None"/> or <c>(1+(<paramref name="m"/>-1)*<paramref name="incx"/>)</c> otherwise</param>
		/// <param name="incx">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="β">The scalar to be multiplied to <paramref name="y"/>. If this is 0, then the original values of <paramref name="y"/> will be ignored.</param>
		/// <param name="y">The input and output vector at least <c>(1+(<paramref name="m"/>-1)*<paramref name="incy"/>)</c> elements if <paramref name="op"/>==<see cref="MatrixOperation.None"/> or <c>(1+(<paramref name="n"/>-1)*<paramref name="incy"/>)</c> otherwise</param>
		/// <param name="incy">The stride between consecutive elements of <paramref name="y"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="incx"/> or <paramref name="incy"/> is less than 1</exception>
		public abstract void GeneralMatrixMultiplyVector<T>(MatrixOperation op, long m, long n, T α, Storage<T> A, long lda, Storage<T> x, int incx, T β, Storage<T> y, int incy) where T : unmanaged, IEquatable<T>;

		/// <summary>
		/// When implemented by a derived class, perform the symmetric/hermitian matrix-vector multiplication:<br/>
		/// <c><paramref name="y"/> = <paramref name="α"/> * <paramref name="A"/>*<paramref name="x"/> + <paramref name="β"/> * <paramref name="y"/></c>.<br/>
		/// Where <paramref name="A"/> is a <paramref name="n"/>×<paramref name="n"/> symmetric/hermitian matrix stored in column-major format, <paramref name="x"/> is a vector, and <paramref name="α"/> is a scalar.
		/// </summary>
		/// <param name="fillLower">The indicates whether <paramref name="A"/>'s lower or upper part is stored</param>
		/// <param name="hermA">Whether <paramref name="A"/> is a hermitian or a symmetric matrix</param>
		/// <param name="n">The number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="α">The scalar used for multiplication</param>
		/// <param name="A">The array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="x">The vector with <c>(1+(<paramref name="n"/>-1)*abs(<paramref name="incx"/>))</c> elements</param>
		/// <param name="incx">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="β">The scalar to be multiplied to <paramref name="y"/>. If this is 0, then the original values of <paramref name="y"/> will be ignored.</param>
		/// <param name="y">The input and output vector at least <c>(1+(<paramref name="n"/>-1)*abs(<paramref name="incy"/>))</c></param>
		/// <param name="incy">The stride between consecutive elements of <paramref name="y"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="incx"/> or <paramref name="incy"/> is less than 1</exception>
		public abstract void SymmHermMatrixMultiplyVector<T>(bool fillLower, bool hermA, long n, T α, Storage<T> A, long lda, Storage<T> x, int incx, T β, Storage<T> y, int incy) where T : unmanaged, IEquatable<T>;

		/// <summary>
		/// When implemented by a derived class, perform the rank-1 update:<br/>
		/// <c><paramref name="A"/> = <paramref name="α"/> * <paramref name="x"/> * <paramref name="y"/><sup>op</sup> + <paramref name="β"/> * <paramref name="A"/></c>, <c>op = <paramref name="conjY"/> ? H : T</c>.<br/>
		/// Where <paramref name="A"/> is a <paramref name="m"/>×<paramref name="n"/> matrix stored in column-major format, <paramref name="x"/> and <paramref name="y"/> are vectors, and <paramref name="α"/> is a scalar.
		/// </summary>
		/// <param name="conjY">Conjugate <paramref name="y"/> or not</param>
		/// <param name="m">The number of rows of matrix <paramref name="A"/></param>
		/// <param name="n">The number of columns of matrix <paramref name="A"/></param>
		/// <param name="α">The scalar used for multiplication</param>
		/// <param name="x">The vector with <c>(1+(<paramref name="m"/>-1)*<paramref name="incx"/>)</c> elements</param>
		/// <param name="incx">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="y">The vector with <c>(1+(<paramref name="n"/>-1)*<paramref name="incy"/>)</c> elements</param>
		/// <param name="incy">The stride between consecutive elements of <paramref name="y"/></param>
		/// <param name="β">The scalar to be multiplied to <paramref name="A"/>. If this is 0, then the original values of <paramref name="A"/> will be ignored.</param>
		/// <param name="A">The input and output array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="y"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="incx"/> or <paramref name="incy"/> is less than 1</exception>
		public abstract void GenralRankOneUpdate<T>(bool conjY, long m, long n, T α, Storage<T> x, int incx, Storage<T> y, int incy, T β, Storage<T> A, long lda) where T : unmanaged, IEquatable<T>;

		/// <summary>
		/// When implemented by a derived class, perform the symmetric/hermitian rank-1 update:<br/>
		/// <c><paramref name="A"/> = <paramref name="α"/> * <paramref name="x"/> * <paramref name="x"/><sup>op</sup> + <paramref name="A"/></c>, <c>op = <paramref name="conjX"/> ? H : T</c>.<br/>
		/// Where <paramref name="A"/> is a <paramref name="n"/>×<paramref name="n"/> symmetric/hermitian matrix stored in column-major format, <paramref name="x"/> is a vector, and <paramref name="α"/> is a scalar.
		/// </summary>
		/// <param name="fillLower">The <see cref="bool"/> of result matrix <paramref name="A"/></param>
		/// <param name="conjX">Conjugate the second <paramref name="x"/> or not</param>
		/// <param name="n">The number of rows and columns of matrix <paramref name="A"/></param>
		/// <param name="α">The scalar used for multiplication</param>
		/// <param name="x">The vector with <c>(1+(<paramref name="n"/>-1)*<paramref name="incx"/>)</c> elements</param>
		/// <param name="incx">The stride between consecutive elements of <paramref name="x"/></param>
		/// <param name="β">The scalar to be multiplied to <paramref name="A"/>. If this is 0, then the original values of <paramref name="A"/> will be ignored.</param>
		/// <param name="A">The array of dimension <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c></param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="x"/> or <paramref name="A"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="incx"/> is less than 1</exception>
		public abstract void SymmHermRankOneUpdate<T>(bool fillLower, bool conjX, long n, T α, Storage<T> x, int incx, T β, Storage<T> A, long lda) where T : unmanaged, IEquatable<T>;
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
		/// <param name="A">The array of dimensions <c><paramref name="lda"/>×<paramref name="k"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c> if <paramref name="opA"/> == <see cref="MatrixOperation.None"/>, and <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="k"/>)</c> otherwise</param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store the matrix <paramref name="A"/></param>
		/// <param name="B">The array of dimension <c><paramref name="ldb"/>×<paramref name="n"/></c> with <c><paramref name="ldb"/> ≥ max(1, <paramref name="k"/>)</c> if <paramref name="opB"/> == <see cref="MatrixOperation.None"/>, and <c><paramref name="ldb"/>×<paramref name="k"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c> otherwise</param>
		/// <param name="ldb">The leading dimension of two-dimensional array used to store matrix <paramref name="B"/></param>
		/// <param name="β">The scalar to be multiplied to <paramref name="C"/>. If this is 0, the original values of <paramref name="C"/> will be ignored.</param>
		/// <param name="C">The array of dimensions <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="ldc">The leading dimension of a two-dimensional array used to store the matrix <paramref name="C"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		public abstract void GeneralMatricesMultiply<T>(MatrixOperation opA, MatrixOperation opB, long m, long n, long k, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc) where T : unmanaged, IEquatable<T>;

		/// <summary>
		/// When implemented by a derived class, perform the symmetric/hermitian matrix-matrix multiplication:<br/>
		/// If <paramref name="leftA"/> is true, <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="A"/> * <paramref name="B"/> + <paramref name="β"/> * <paramref name="C"/></c>; otherwise, <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="B"/> * <paramref name="A"/> + <paramref name="β"/> * <paramref name="C"/></c>.<br/>
		/// Where <paramref name="A"/> is a symmetric/hermitian matrix stored in lower or upper mode, <paramref name="B"/> and <paramref name="C"/> are <paramref name="m"/>×<paramref name="n"/> matrices, and <paramref name="α"/> and <paramref name="β"/> are scalars.
		/// </summary>
		/// <param name="fillLower">The <see cref="bool"/> indicates whether matrix <paramref name="A"/> lower or upper part is stored</param>
		/// <param name="leftA">The <see cref="bool"/> indicates whether matrix <paramref name="A"/> is on the left or right of <paramref name="B"/></param>
		/// <param name="hermA">Whether <paramref name="A"/> is a hermitian or symmetric matrix</param>
		/// <param name="m">The number of rows of matrix <paramref name="C"/> and <paramref name="B"/>, with matrix <paramref name="A"/> sized accordingly</param>
		/// <param name="n">The number of columns of matrix <paramref name="C"/> and <paramref name="B"/>, with matrix <paramref name="A"/> sized accordingly</param>
		/// <param name="α">The scalar to be multiplied to <paramref name="A"/></param>
		/// <param name="A">The symmetric/Hermitian matrix of dimension <c><paramref name="lda"/>×<paramref name="m"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="m"/>)</c> if <paramref name="leftA"/> is true, and <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c> otherwise</param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="B">The array of dimension <c><paramref name="ldb"/>×<paramref name="n"/></c> with <c><paramref name="ldb"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="ldb">The leading dimension of two-dimensional array used to store matrix <paramref name="B"/></param>
		/// <param name="β">The scalar to be multiplied by <paramref name="C"/>. If it is 0, the original values of <paramref name="C"/> will be ignored.</param>
		/// <param name="C">The array of dimension <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(1, <paramref name="m"/>)</c></param>
		/// <param name="ldc">The leading dimension of two-dimensional array used to store matrix <paramref name="B"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="B"/> or <paramref name="C"/> is null or invalid</exception>
		public abstract void SymmHermMatrixMultiplyGeneral<T>(bool fillLower, bool leftA, bool hermA, long m, long n, T α, Storage<T> A, long lda, Storage<T> B, long ldb, T β, Storage<T> C, long ldc) where T : unmanaged, IEquatable<T>;

		/// <summary>
		/// When implemented by a derived class, perform the symmetric/hermitian rank-k update:<br/>
		/// <c><paramref name="C"/> = <paramref name="α"/> * <paramref name="op"/>(<paramref name="A"/>) * <paramref name="op"/>(<paramref name="A"/>)<sup>pow</sup> + <paramref name="β"/> * <paramref name="C"/></c>, <c>pow = <paramref name="conjA"/> ? H : T</c>.<br/>
		/// Where <paramref name="α"/> and <paramref name="β"/> are scalars, <paramref name="C"/> is a symmetric/hermitian matrix stored in lower or upper mode, and <paramref name="A"/> is a matrix with dimensions <paramref name="op"/>(<paramref name="A"/>) → <paramref name="n"/>×<paramref name="k"/>.
		/// </summary>
		/// <param name="fillLower">The <see cref="bool"/> indicates whether matrix <paramref name="C"/>'s lower or upper part is stored</param>
		/// <param name="op">The <see cref="MatrixOperation"/> indicates the simple operation to <paramref name="A"/></param>
		/// <param name="conjA">Conjugate transpose <paramref name="A"/> or just transpose <paramref name="A"/></param>
		/// <param name="n">The number of rows of matrix <paramref name="op"/>(<paramref name="A"/>) and <paramref name="C"/></param>
		/// <param name="k">The number of columns of matrix <paramref name="op"/>(<paramref name="A"/>)</param>
		/// <param name="α">The scalar to be multiplied to <paramref name="op"/>(<paramref name="A"/>) * <paramref name="op"/>(<paramref name="A"/>)<sup>pow</sup></param>
		/// <param name="A">The array of dimension <c><paramref name="lda"/>×<paramref name="k"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="n"/>)</c> if trans == <see cref="MatrixOperation.None"/> and <c><paramref name="lda"/>×<paramref name="n"/></c> with <c><paramref name="lda"/> ≥ max(1, <paramref name="k"/>)</c> otherwise</param>
		/// <param name="lda">The leading dimension of two-dimensional array used to store matrix <paramref name="A"/></param>
		/// <param name="β">The scalar to be multiplied by <paramref name="C"/>. If it is 0, the original values of <paramref name="C"/> will be ignored.</param>
		/// <param name="C">The symmetric/hermitian matrix of dimension <c><paramref name="ldc"/>×<paramref name="n"/></c> with <c><paramref name="ldc"/> ≥ max(1, <paramref name="n"/>)</c></param>
		/// <param name="ldc">The leading dimension of two-dimensional array used to store matrix <paramref name="C"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="A"/> or <paramref name="C"/> is null or invalid</exception>
		public abstract void RankKUpdate<T>(bool fillLower, MatrixOperation op, bool conjA, long n, long k, T α, Storage<T> A, long lda, T β, Storage<T> C, long ldc) where T : unmanaged, IEquatable<T>;
		#endregion
	}
}