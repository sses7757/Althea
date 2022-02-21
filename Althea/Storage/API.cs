using System;

using Althea.NativeTypes;
using Althea.Resources;

using Althea.SourceGenerator;


namespace Althea.Storage
{
	/// <summary>
	/// The abstract class for runtime memory API routines 
	/// </summary>
	[AbstractRuntimeApi]
	public abstract class AbstractApi : AbstractRuntimeApi<AbstractApi>
	{
		#region storage operations
		/// <summary>
		/// When implemented by a derived class, allocate a storage of type <typeparamref name="TP"/> with given <paramref name="length"/> in bytes.
		/// </summary>
		/// <typeparam name="TP">The pointer type that implements <see cref="IPointer{TSelf}"/></typeparam>
		/// <param name="length">The length to allocate in bytes</param>
		/// <param name="result">The result -- an allocated pointer as a <typeparamref name="TP"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="OutOfMemoryException">If <paramref name="length"/> is too large to be allocated</exception>
		[AbstractApiMethod(true)]
		public abstract bool Allocate<TP>([DuplicateTParameter] long length, out TP result) where TP : IPointer<TP>;

		/// <summary>
		/// When implemented by a derived class, free a storage indicated by a given <paramref name="pointer"/>.
		/// </summary>
		/// <typeparam name="TP">The pointer type that implements <see cref="IPointer{TSelf}"/></typeparam>
		/// <param name="pointer">The <typeparamref name="TP"/> to free</param>
		/// <param name="valid">If <paramref name="pointer"/> is not valid, output false; otherwise, output true</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		[AbstractApiMethod]
		public abstract bool Free<TP>(TP pointer, out bool valid) where TP : IPointer<TP>;

		/// <summary>
		/// When implemented by a derived class, fill the <paramref name="pointer"/> by same <paramref name="value"/>, byte by byte.
		/// </summary>
		/// <typeparam name="TP">The pointer type that implements <see cref="IPointer{TSelf}"/></typeparam>
		/// <param name="pointer">The pointer to be filled</param>
		/// <param name="value">The value to set as a <see cref="byte"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pointer"/> is invalid</exception>
		[AbstractApiMethod]
		public abstract bool FillWithValue<TP>(PointerSegment<TP> pointer, byte value) where TP : IPointer<TP>;

		/// <summary>
		/// When implemented by a derived class, fill the <paramref name="pointer"/>'s each value by same <paramref name="value"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TP">The pointer type that implements <see cref="IPointer{TSelf}"/></typeparam>
		/// <param name="pointer">The pointer to be filled</param>
		/// <param name="value">The value to set as a <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pointer"/> is invalid</exception>
		[AbstractApiMethod]
		public abstract bool FillWithValue<T, TP>(PointerSegment<TP> pointer, T value) where T : unmanaged, INumber<T> where TP : IPointer<TP>;

		/// <summary>
		/// When implemented by a derived class, copy memory from <paramref name="source"/> to <paramref name="destination"/>.
		/// </summary>
		/// <typeparam name="TP1">A pointer type that implements <see cref="IPointer{TSelf}"/></typeparam>
		/// <typeparam name="TP2">A pointer type that implements <see cref="IPointer{TSelf}"/></typeparam>
		/// <param name="source">The source pointer to copy from</param>
		/// <param name="destination">The destination pointer to copy into</param>
		/// <param name="actualCopied">Output the number of bytes of actually copied block</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <remarks>The one with less length in <paramref name="source"/> and <paramref name="destination"/> is used as the actual copy length in bytes</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is invalid</exception>
		[AbstractApiMethod(true)]
		public abstract bool MemoryCopy<TP1, TP2>(PointerSegment<TP1> source, PointerSegment<TP2> destination, [DuplicateTParameter] out long actualCopied) where TP1 : IPointer<TP1> where TP2 : IPointer<TP2>;

		/// <summary>
		/// When implemented by a derived class, copy 2D data from <paramref name="source"/> to <paramref name="destination"/>.
		/// </summary>
		/// <typeparam name="TP1">A pointer type that implements <see cref="IPointer{TSelf}"/></typeparam>
		/// <typeparam name="TP2">A pointer type that implements <see cref="IPointer{TSelf}"/></typeparam>
		/// <param name="source">The source pointer</param>
		/// <param name="sourceLD">The source array actual height (actual leading dimension) in bytes</param>
		/// <param name="destination">The destination pointer</param>
		/// <param name="destinationLD">The destination array actual height (actual leading dimension) in bytes</param>
		/// <param name="height">The height to copy in bytes</param>
		/// <param name="width">The width to copy in the real type</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <remarks>The lengths of <paramref name="source"/> and <paramref name="destination"/> are ignored</remarks>
		/// <exception cref="ArgumentOutOfRangeException">If any of the parameters is zero</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is invalid</exception>
		/// <exception cref="ArgumentException">
		/// If <paramref name="height"/> is larger than <paramref name="sourceLD"/> or <paramref name="destinationLD"/>,
		/// or <paramref name="height"/> is larger than <paramref name="sourceLD"/> or <paramref name="destinationLD"/>,
		/// or <c><paramref name="sourceLD"/> * <paramref name="width"/> &gt; <paramref name="source"/>.<see cref="IStorage.LengthInBytes">Length</see></c>, 
		/// or <c><paramref name="destinationLD"/> * <paramref name="width"/> &gt; <paramref name="destination"/>.<see cref="IStorage.LengthInBytes">Length</see></c>
		/// </exception>
		[AbstractApiMethod(true)]
		public abstract bool MemoryCopy2D<TP1, TP2>(PointerSegment<TP1> source, [DuplicateTParameter] long sourceLD, PointerSegment<TP2> destination, [DuplicateTParameter] long destinationLD, [DuplicateTParameter] long height, [DuplicateTParameter] long width) where TP1 : IPointer<TP1> where TP2 : IPointer<TP2>;

		/// <summary>
		/// When implemented by a derived class, copy the <paramref name="source"/> storage to <paramref name="destination"/> storage with given strides.<br/>
		/// <c><paramref name="destination"/>[j] = <paramref name="source"/>[k] for i = 1,¡­,n; k = 1 + (i - 1)*<paramref name="incrementSource"/>, j = 1 + (i - 1)*<paramref name="incrementDestination"/></c>.<br/>
		/// The number of elements copied is calculated to the maximum possible value that does not exceeds the boundaries.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TP1">A pointer type that implements <see cref="IPointer{TSelf}"/></typeparam>
		/// <typeparam name="TP2">A pointer type that implements <see cref="IPointer{TSelf}"/></typeparam>
		/// <param name="source">The source pointer to copy from</param>
		/// <param name="incrementSource">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="source"/></param>
		/// <param name="destination">The destination pointer to copy to</param>
		/// <param name="incrementDestination">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="destination"/></param>
		/// <param name="actualCopied">Output the number of elements (in <typeparamref name="T"/>) actually copied</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/>is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="incrementSource"/> or <paramref name="incrementDestination"/> is less than 1</exception>
		[AbstractApiMethod]
		public abstract bool StridedCopy<T, TP1, TP2>(PointerSegment<TP1> source, int incrementSource, PointerSegment<TP2> destination, int incrementDestination, out long actualCopied) where T : unmanaged, INumber<T> where TP1 : IPointer<TP1> where TP2 : IPointer<TP2>;
		#endregion

		#region storage and managed operations
		/// <summary>
		/// When implemented by a derived class, copy out the <b>first</b> element in unmanaged pointer <paramref name="source"/> to a managed value of type <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TP">The pointer type that implements <see cref="IPointer{TSelf}"/></typeparam>
		/// <param name="source">The source pointer to copy from</param>
		/// <param name="value">Output the first element in <paramref name="source"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is invalid</exception>
		[AbstractApiMethod]
		public abstract bool ToManaged<T, TP>(PointerSegment<TP> source, out T value) where T : unmanaged, INumber<T> where TP : IPointer<TP>;

		/// <summary>
		/// When implemented by a derived class, overwrite the <b>first</b> element in unmanaged pointer <paramref name="destination"/> by a managed <paramref name="value"/> of type <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TP">The pointer type that implements <see cref="IPointer{TSelf}"/></typeparam>
		/// <param name="destination">The destination pointer to copy to</param>
		/// <param name="value">The value of type <typeparamref name="T"/> to copy from</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="destination"/> is invalid</exception>
		[AbstractApiMethod]
		public abstract bool FromManaged<T, TP>(PointerSegment<TP> destination, T value) where T : unmanaged, INumber<T> where TP : IPointer<TP>;

		/// <summary>
		/// When implemented by a derived class, copy out the first few elements in unmanaged pointer <paramref name="source"/> to a managed array of type <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TP">The pointer type that implements <see cref="IPointer{TSelf}"/></typeparam>
		/// <param name="source">The source pointer to copy from</param>
		/// <param name="destination">The managed <see cref="Span{T}"/> of type <typeparamref name="T"/> to copy to</param>
		/// <param name="actualCopied">Output the number of elements (in <typeparamref name="T"/>) actually copied</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is invalid</exception>
		[AbstractApiMethod]
		public abstract bool ToManaged<T, TP>(PointerSegment<TP> source, Span<T> destination, out long actualCopied) where T : unmanaged, INumber<T> where TP : IPointer<TP>;

		/// <summary>
		/// When implemented by a derived class, overwrite the first few elements in unmanaged pointer <paramref name="destination"/> by the <paramref name="values"/> of a managed array of type <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TP">The pointer type that implements <see cref="IPointer{TSelf}"/></typeparam>
		/// <param name="destination">The destination pointer to copy to</param>
		/// <param name="values">The managed <see cref="ReadOnlySpan{T}"/> of type <typeparamref name="T"/> to copy from</param>
		/// <param name="actualCopied">Output the number of elements (in <typeparamref name="T"/>) actually copied</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="destination"/> is invalid</exception>
		[AbstractApiMethod]
		public abstract bool FromManaged<T, TP>(PointerSegment<TP> destination, ReadOnlySpan<T> values, out long actualCopied) where T : unmanaged, INumber<T> where TP : IPointer<TP>;

		// Ignore Spelling: sizeof
		/// <summary>
		/// When implemented by a derived class, copy out the elements in unmanaged pointer <paramref name="source"/> as a 2D matrix to a managed array of type <typeparamref name="T"/> (viewed as a 1D array).
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TP">The pointer type that implements <see cref="IPointer{TSelf}"/></typeparam>
		/// <param name="source">The source pointer to copy from</param>
		/// <param name="leadDim">The actual height (leading dimension) in <typeparamref name="T"/> of <paramref name="source"/></param>
		/// <param name="height">The height to copy in <typeparamref name="T"/></param>
		/// <param name="width">The width to copy in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="destination">The managed <see cref="Span{T}"/> of type <typeparamref name="T"/> to copy to, must has <c><see cref="Array.Length">Length</see> ¡Ý <paramref name="height"/> <paramref name="width"/></c></param>
		/// <param name="destinationLeadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="destination"/>, default 0 means <paramref name="height"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is invalid</exception>
		/// <exception cref="ArgumentException">
		/// If <paramref name="height"/> is larger than <paramref name="leadDim"/> or <paramref name="destinationLeadDim"/>,
		/// or <c><paramref name="leadDim"/> * <paramref name="width"/> * sizeof(<typeparamref name="T"/>) &gt; <paramref name="source"/>.<see cref="PointerSegment{TP}.LengthInBytes">Length</see></c>,
		/// or <c><paramref name="destinationLeadDim"/> * <paramref name="width"/> &gt; <paramref name="destination"/></c>.<see cref="Span{T}.Length">Length</see>
		/// </exception>
		[AbstractApiMethod]
		public abstract bool ToManaged2D<T, TP>(PointerSegment<TP> source, long leadDim, long height, long width, Span<T> destination, long destinationLeadDim = 0) where T : unmanaged, INumber<T> where TP : IPointer<TP>;

		/// <summary>
		/// When implemented by a derived class, overwrite some of the elements in unmanaged pointer <paramref name="destination"/> as a 2D matrix by  a managed array of type <typeparamref name="T"/> (viewed as a 1D array).
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TP">The pointer type that implements <see cref="IPointer{TSelf}"/></typeparam>
		/// <param name="destination">The destination pointer to copy to</param>
		/// <param name="leadDim">The actual height (leading dimension) in <typeparamref name="T"/> of <paramref name="destination"/></param>
		/// <param name="height">The height to copy in <typeparamref name="T"/></param>
		/// <param name="width">The width to copy in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="values">The managed <see cref="ReadOnlySpan{T}"/> of type <typeparamref name="T"/> to copy from, must has <c><see cref="Array.Length">Length</see> ¡Ý <paramref name="height"/> * <paramref name="width"/></c></param>
		/// <param name="valuesLeadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="values"/>, default 0 means <paramref name="height"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="destination"/> is invalid</exception>
		/// <exception cref="ArgumentException">
		/// If <paramref name="height"/> is larger than <paramref name="leadDim"/> or <paramref name="valuesLeadDim"/>,
		/// or <c><paramref name="leadDim"/> * <paramref name="width"/> * sizeof(<typeparamref name="T"/>) &gt; <paramref name="destination"/>.<see cref="PointerSegment{TP}.LengthInBytes">Length</see></c>,
		/// or <c><paramref name="valuesLeadDim"/> * <paramref name="width"/> &gt; <paramref name="values"/></c>.<see cref="ReadOnlySpan{T}.Length">Length</see>
		/// </exception>
		[AbstractApiMethod]
		public abstract bool FromManaged2D<T, TP>(PointerSegment<TP> destination, long leadDim, long height, long width, ReadOnlySpan<T> values, long valuesLeadDim = 0) where T : unmanaged, INumber<T> where TP : IPointer<TP>;
		#endregion
	}

	/// <summary>
	/// The API selector class for runtime memory API routines
	/// </summary>
	public sealed partial class ApiSelector : AbstractApiSelector<AbstractApi>
	{
		#region extensions
		public static void MemoryCopy<T, TS>(TS source, TS destination) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
		{

		}
		#endregion
	}
}