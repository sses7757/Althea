using System.Reflection;

using Althea.Resources;
using Althea.SourceGenerator;
using Althea.Storage;


namespace Althea.LinearAlgebra.Dense
{
	/// <summary>
	/// The abstract interface for dense linear algebra copy API routines 
	/// </summary>
	[AbstractRuntimeApi]
	public interface ICopyAbstractApi : IAbstractRuntimeApi<ICopyAbstractApi>
	{
		#region storage operations
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
		/// or <c><paramref name="sourceLD"/> and <paramref name="width"/> indicate size larger than <paramref name="source"/>.<see cref="IStorage.LengthInBytes">Length</see></c>, 
		/// or <c><paramref name="destinationLD"/> and <paramref name="width"/> indicate size larger than <paramref name="destination"/>.<see cref="IStorage.LengthInBytes">Length</see></c>
		/// </exception>
		[AbstractApiMethod(true)]
		public abstract bool MemoryCopy2D<TP1, TP2>(PointerSegment<TP1> source, [DuplicateTParameter] long sourceLD, PointerSegment<TP2> destination, [DuplicateTParameter] long destinationLD, [DuplicateTParameter] long height, [DuplicateTParameter] long width) where TP1 : IPointer<TP1> where TP2 : IPointer<TP2>;

		/// <summary>
		/// When implemented by a derived class, copy the <paramref name="source"/> storage to <paramref name="destination"/> storage with given strides.<br/>
		/// <c><paramref name="destination"/>[j] = <paramref name="source"/>[k] for i = 0, ..., n - 1; k = i * <paramref name="strideSource"/>, j = i * <paramref name="strideDestination"/></c>.<br/>
		/// The number of elements copied is calculated to the maximum possible value that does not exceeds the boundaries.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TP1">A pointer type that implements <see cref="IPointer{TSelf}"/></typeparam>
		/// <typeparam name="TP2">A pointer type that implements <see cref="IPointer{TSelf}"/></typeparam>
		/// <param name="source">The source pointer to copy from</param>
		/// <param name="strideSource">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="source"/></param>
		/// <param name="destination">The destination pointer to copy to</param>
		/// <param name="strideDestination">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="destination"/></param>
		/// <param name="actualCopied">Output the number of elements (in <typeparamref name="T"/>) actually copied</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideSource"/> or <paramref name="strideDestination"/> ≤ 0</exception>
		[AbstractApiMethod]
		public abstract bool StridedCopy<T, TP1, TP2>(PointerSegment<TP1> source, long strideSource, PointerSegment<TP2> destination, long strideDestination, out long actualCopied) where T : unmanaged, INumber<T> where TP1 : IPointer<TP1> where TP2 : IPointer<TP2>;
		#endregion

		////#region storage and managed operations
		////// Ignore Spelling: sizeof
		/////// <summary>
		/////// When implemented by a derived class, copy out the elements in unmanaged pointer <paramref name="source"/> as a 2D matrix to a managed array of type <typeparamref name="T"/> (viewed as a 1D array).
		/////// </summary>
		/////// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/////// <typeparam name="TP">The pointer type that implements <see cref="IPointer{TSelf}"/></typeparam>
		/////// <param name="source">The source pointer to copy from</param>
		/////// <param name="leadDim">The actual height (leading dimension) in <typeparamref name="T"/> of <paramref name="source"/></param>
		/////// <param name="height">The height to copy in <typeparamref name="T"/></param>
		/////// <param name="width">The width to copy in <typeparamref name="T"/> rather than bytes</param>
		/////// <param name="destination">The managed <see cref="Span{T}"/> of type <typeparamref name="T"/> to copy to, must has <c><see cref="Array.Length">Length</see> ≥ <paramref name="height"/> <paramref name="width"/></c></param>
		/////// <param name="destinationLeadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="destination"/>, default 0 means <paramref name="height"/></param>
		/////// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/////// <exception cref="ArgumentNullException">If <paramref name="source"/> is invalid</exception>
		/////// <exception cref="ArgumentException">
		/////// If <paramref name="height"/> is larger than <paramref name="leadDim"/> or <paramref name="destinationLeadDim"/>,
		/////// or <c><paramref name="leadDim"/> * <paramref name="width"/> * sizeof(<typeparamref name="T"/>) &gt; <paramref name="source"/>.<see cref="PointerSegment{TP}.LengthInBytes">Length</see></c>,
		/////// or <c><paramref name="destinationLeadDim"/> * <paramref name="width"/> &gt; <paramref name="destination"/></c>.<see cref="Span{T}.Length">Length</see>
		/////// </exception>
		////[AbstractApiMethod]
		////public unsafe virtual bool ToManaged2D<T, TP>(PointerSegment<TP> source, long leadDim, long height, long width, Span<T> destination, long destinationLeadDim = 0) where T : unmanaged, INumber<T> where TP : IPointer<TP>
		////{
		////	if (destination.IsEmpty)
		////		throw new ArgumentNullException(nameof(destination));
		////	if (destinationLeadDim == 0)
		////		destinationLeadDim = height;
		////	leadDim *= sizeof(T); height *= sizeof(T); width *= sizeof(T); destinationLeadDim *= sizeof(T);
		////	fixed (T* dst = destination)
		////	{
		////		ManagedPointer mp = new(new(dst), sizeof(T) * destination.Length);
		////		return this.MemoryCopy2D<TP, ManagedPointer>(source, leadDim, mp, destinationLeadDim, height, width);
		////	}
		////}

		/////// <summary>
		/////// When implemented by a derived class, overwrite some of the elements in unmanaged pointer <paramref name="destination"/> as a 2D matrix by a managed array of type <typeparamref name="T"/> (viewed as a 1D array).
		/////// </summary>
		/////// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/////// <typeparam name="TP">The pointer type that implements <see cref="IPointer{TSelf}"/></typeparam>
		/////// <param name="destination">The destination pointer to copy to</param>
		/////// <param name="leadDim">The actual height (leading dimension) in <typeparamref name="T"/> of <paramref name="destination"/></param>
		/////// <param name="height">The height to copy in <typeparamref name="T"/></param>
		/////// <param name="width">The width to copy in <typeparamref name="T"/> rather than bytes</param>
		/////// <param name="values">The managed <see cref="ReadOnlySpan{T}"/> of type <typeparamref name="T"/> to copy from, must has <c><see cref="Array.Length">Length</see> ≥ <paramref name="height"/> * <paramref name="width"/></c></param>
		/////// <param name="valuesLeadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="values"/>, default 0 means <paramref name="height"/></param>
		/////// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/////// <exception cref="ArgumentNullException">If <paramref name="destination"/> is invalid</exception>
		/////// <exception cref="ArgumentException">
		/////// If <paramref name="height"/> is larger than <paramref name="leadDim"/> or <paramref name="valuesLeadDim"/>,
		/////// or <c><paramref name="leadDim"/> * <paramref name="width"/> * sizeof(<typeparamref name="T"/>) &gt; <paramref name="destination"/>.<see cref="PointerSegment{TP}.LengthInBytes">Length</see></c>,
		/////// or <c><paramref name="valuesLeadDim"/> * <paramref name="width"/> &gt; <paramref name="values"/></c>.<see cref="ReadOnlySpan{T}.Length">Length</see>
		/////// </exception>
		////[AbstractApiMethod]
		////public unsafe virtual bool FromManaged2D<T, TP>(PointerSegment<TP> destination, long leadDim, long height, long width, ReadOnlySpan<T> values, long valuesLeadDim = 0) where T : unmanaged, INumber<T> where TP : IPointer<TP>
		////{
		////	if (values.IsEmpty)
		////		throw new ArgumentNullException(nameof(values));
		////	if (valuesLeadDim == 0)
		////		valuesLeadDim = height;
		////	leadDim *= sizeof(T); height *= sizeof(T); width *= sizeof(T); valuesLeadDim *= sizeof(T);
		////	fixed (T* src = values)
		////	{
		////		ManagedPointer mp = new(new(src), sizeof(T) * values.Length);
		////		return this.MemoryCopy2D<ManagedPointer, TP>(mp, valuesLeadDim, destination, leadDim, height, width);
		////	}
		////}

		/////// <summary>
		/////// When implemented by a derived class, copy the <paramref name="source"/> storage to <paramref name="destination"/> which is a managed array of type <typeparamref name="T"/> with given strides.<br/>
		/////// <c><paramref name="destination"/>[j] = <paramref name="source"/>[k] for i = 0, ..., n - 1; k = i * <paramref name="strideSource"/>, j = i * <paramref name="strideDestination"/></c>.<br/>
		/////// The number of elements copied is calculated to the maximum possible value that does not exceeds the boundaries.
		/////// </summary>
		/////// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/////// <typeparam name="TP">A pointer type that implements <see cref="IPointer{TSelf}"/></typeparam>
		/////// <param name="source">The source pointer to copy from</param>
		/////// <param name="strideSource">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="source"/></param>
		/////// <param name="destination">The destination managed array to copy to</param>
		/////// <param name="strideDestination">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="destination"/></param>
		/////// <param name="actualCopied">Output the number of elements (in <typeparamref name="T"/>) actually copied</param>
		/////// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/////// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is invalid</exception>
		/////// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideSource"/> or <paramref name="strideDestination"/> ≤ 0</exception>
		////[AbstractApiMethod]
		////public unsafe virtual bool ToManagedStrided<T, TP>(PointerSegment<TP> source, long strideSource, Span<T> destination, long strideDestination, out long actualCopied) where T : unmanaged, INumber<T> where TP : IPointer<TP>
		////{
		////	if (destination.IsEmpty)
		////		throw new ArgumentNullException(nameof(destination));
		////	fixed (T* dst = destination)
		////	{
		////		ManagedPointer mp = new(new(dst), sizeof(T) * destination.Length);
		////		return this.StridedCopy<T, TP, ManagedPointer>(source, strideSource, mp, strideDestination, out actualCopied);
		////	}
		////}

		/////// <summary>
		/////// When implemented by a derived class, copy some of the values in the <paramref name="values"/> managed array of type <typeparamref name="T"/> to <paramref name="destination"/> storage with given strides.<br/>
		/////// <c><paramref name="destination"/>[j] = <paramref name="values"/>[k] for i = 0, ..., n - 1; k = i * <paramref name="strideValues"/>, j = i * <paramref name="strideDestination"/></c>.<br/>
		/////// The number of elements copied is calculated to the maximum possible value that does not exceeds the boundaries.
		/////// </summary>
		/////// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/////// <typeparam name="TP">A pointer type that implements <see cref="IPointer{TSelf}"/></typeparam>
		/////// <param name="values">The source managed array to copy from</param>
		/////// <param name="strideValues">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="values"/></param>
		/////// <param name="destination">The destination storage to copy to</param>
		/////// <param name="strideDestination">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="destination"/></param>
		/////// <param name="actualCopied">Output the number of elements (in <typeparamref name="T"/>) actually copied</param>
		/////// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/////// <exception cref="ArgumentNullException">If <paramref name="values"/> or <paramref name="destination"/> is invalid</exception>
		/////// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideValues"/> or <paramref name="strideDestination"/> ≤ 0</exception>
		////[AbstractApiMethod]
		////public unsafe virtual bool FromManagedStrided<T, TP>(PointerSegment<TP> destination, long strideDestination, Span<T> values, long strideValues, out long actualCopied) where T : unmanaged, INumber<T> where TP : IPointer<TP>
		////{
		////	if (values.IsEmpty)
		////		throw new ArgumentNullException(nameof(values));
		////	fixed (T* src = values)
		////	{
		////		ManagedPointer mp = new(new(src), sizeof(T) * values.Length);
		////		return this.StridedCopy<T, ManagedPointer, TP>(mp, strideValues, destination, strideDestination, out actualCopied);
		////	}
		////}
		////#endregion
	}


	/// <summary>
	/// The static class for extension methods for <see cref="IStorage"/> and <see cref="IStorage{T, TSelf}"/>
	/// </summary>
	public static class StorageExtension
	{
		#region method generators
#pragma warning disable CS8601
		internal static readonly MethodInfo SizeOfPointerMethod = typeof(IStorage).GetMethod(nameof(IStorage.SizeOfPointer), 0, BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(int) }, null);
#pragma warning restore CS8601
		private static Action<TS1, TS2, long, long, long, long> GetCopy2DMethod<TS1, TS2>() where TS1 : class, IStorage where TS2 : class, IStorage
		{
			return default;
			// TODO
		}
		private static Action<TS1, TS2, long, long> GetCopyStridedMethod<TS1, TS2>() where TS1 : class, IStorage where TS2 : class, IStorage
		{
			return default;
			// TODO
		}
		#endregion

		#region extension methods
		private static readonly Dictionary<(RuntimeTypeHandle, RuntimeTypeHandle), Delegate> copy2DFunc = new();

		/// <summary>
		/// Copy the data from <paramref name="source"/> storage to <paramref name="destination"/> storage in 2D/matrix form with <paramref name="height"/> and <paramref name="width"/>.
		/// </summary>
		/// <typeparam name="T">The data type of <typeparamref name="TS1"/> and <typeparamref name="TS2"/></typeparam>
		/// <typeparam name="TS1">The source storage class that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The destination storage class that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="source">The source storage</param>
		/// <param name="destination">The destination storage</param>
		/// <param name="sourceLD">The source array's actual height (actual leading dimension) in <typeparamref name="T"/></param>
		/// <param name="destinationLD">The destination array's actual height (actual leading dimension) in <typeparamref name="T"/></param>
		/// <param name="height">The height to copy in <typeparamref name="T"/></param>
		/// <param name="width">The width to copy in <typeparamref name="T"/></param>
		/// <exception cref="ArgumentOutOfRangeException">If any of the parameters is zero</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is invalid</exception>
		/// <exception cref="ArgumentException">
		/// If <paramref name="height"/> is larger than <paramref name="sourceLD"/> or <paramref name="destinationLD"/>,
		/// or <c><paramref name="sourceLD"/> and <paramref name="width"/> indicate size larger than <paramref name="source"/>.<see cref="IStorage.LengthInBytes">Length</see></c>, 
		/// or <c><paramref name="destinationLD"/> and <paramref name="width"/> indicate size larger than <paramref name="destination"/>.<see cref="IStorage.LengthInBytes">Length</see></c>
		/// </exception>
		/// <exception cref="InvalidOperationException">If <paramref name="source"/> overlaps with <paramref name="destination"/> or the <see cref="IStorage.PointerGetters"/> of <typeparamref name="TS1"/> or <typeparamref name="TS2"/> are not correct pointer property names</exception>
		public static void Copy2DTo<T, TS1, TS2>(this TS1 source, long sourceLD, TS2 destination, long destinationLD, long height, long width) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			if (!source.IsValid())
				throw new ArgumentNullException(nameof(source));
			if (!destination.IsValid())
				throw new ArgumentNullException(nameof(destination));
			if (source.OverlapWith(destination))
				throw new InvalidOperationException(StorageError.CannotCopyOverlap);
			if (sourceLD == destinationLD && sourceLD == height)
			{
				source.CopyTo<T, TS1, TS2>(destination.MakeReference(0, height * width));
				return;
			}

			RuntimeTypeHandle handle1 = typeof(TS1).TypeHandle, handle2 = typeof(TS2).TypeHandle;
			if (copy2DFunc.TryGetValue((handle1, handle2), out var copier))
				goto FINAL;
			copier = GetCopy2DMethod<TS1, TS2>();
			copy2DFunc[(handle1, handle2)] = copier;
		FINAL:
			((Action<TS1, TS2, long, long, long, long>)copier).Invoke(source, destination, sourceLD, destinationLD, height, width);
		}

		/// <summary>
		/// Copy the data from <paramref name="source"/> storage to <paramref name="destination"/> span in 2D/matrix form with <paramref name="height"/> and <paramref name="width"/>.
		/// </summary>
		/// <typeparam name="T">The data type of <typeparamref name="TS"/> and <paramref name="destination"/></typeparam>
		/// <typeparam name="TS">The source storage class that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="source">The source storage</param>
		/// <param name="destination">The destination span</param>
		/// <param name="sourceLD">The source array's actual height (actual leading dimension) in <typeparamref name="T"/></param>
		/// <param name="destinationLD">The destination array's actual height (actual leading dimension) in <typeparamref name="T"/>, default 0 means the same as <paramref name="height"/></param>
		/// <param name="height">The height to copy in <typeparamref name="T"/></param>
		/// <param name="width">The width to copy in <typeparamref name="T"/></param>
		public static unsafe void ToManaged2D<T, TS>(this TS source, long sourceLD, Span<T> destination, long height, long width, long destinationLD = 0) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
		{
			if (destinationLD == 0)
				destinationLD = height;
			fixed (T* dst = destination)
			{
				ManagedPointer mp = new(new(dst), destination.Length * sizeof(T));
				ManagedPureStorage<T> ms = new(mp);
				Copy2DTo<T, TS, ManagedPureStorage<T>>(source, sourceLD, ms, destinationLD, height, width);
			}
		}

		/// <summary>
		/// Copy the data from <paramref name="source"/> span to <paramref name="destination"/> storage in 2D/matrix form with <paramref name="height"/> and <paramref name="width"/>.
		/// </summary>
		/// <typeparam name="T">The data type of <typeparamref name="TS"/> and <paramref name="destination"/></typeparam>
		/// <typeparam name="TS">The source storage class that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="source">The source span</param>
		/// <param name="destination">The destination storage</param>
		/// <param name="sourceLD">The source array's actual height (actual leading dimension) in <typeparamref name="T"/>, default 0 means the same as <paramref name="height"/></param>
		/// <param name="destinationLD">The destination array's actual height (actual leading dimension) in <typeparamref name="T"/></param>
		/// <param name="height">The height to copy in <typeparamref name="T"/></param>
		/// <param name="width">The width to copy in <typeparamref name="T"/></param>
		public static unsafe void FromManaged2D<T, TS>(this Span<T> source, long destinationLD, TS destination, long height, long width, long sourceLD = 0) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
		{
			if (sourceLD == 0)
				sourceLD = height;
			fixed (T* src = source)
			{
				ManagedPointer mp = new(new(src), source.Length * sizeof(T));
				ManagedPureStorage<T> ms = new(mp);
				Copy2DTo<T, ManagedPureStorage<T>, TS>(ms, sourceLD, destination, destinationLD, height, width);
			}
		}

		private static readonly Dictionary<(RuntimeTypeHandle, RuntimeTypeHandle), Delegate> copyStridedFunc = new();

		/// <summary>
		/// Copy the <paramref name="source"/> storage to <paramref name="destination"/> storage with given strides.<br/>
		/// <c><paramref name="destination"/>[j] = <paramref name="source"/>[k] for i = 0, ..., n - 1; k = i * <paramref name="strideSource"/>, j = i * <paramref name="strideDestination"/></c>.<br/>
		/// The number of elements copied is calculated to the maximum possible value that does not exceeds the boundaries.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS1">The source storage class that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The destination storage class that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="source">The source storage to copy from</param>
		/// <param name="strideSource">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="source"/></param>
		/// <param name="destination">The destination storage to copy to</param>
		/// <param name="strideDestination">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="destination"/></param>
		/// <returns>The number of elements (in <typeparamref name="T"/>) actually copied.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideSource"/> or <paramref name="strideDestination"/> ≤ 0</exception>
		public static long StridedCopyTo<T, TS1, TS2>(this TS1 source, long strideSource, TS2 destination, long strideDestination) where T : unmanaged, INumber<T> where TS1 : class, IStorage<T, TS1> where TS2 : class, IStorage<T, TS2>
		{
			if (!source.IsValid())
				throw new ArgumentNullException(nameof(source));
			if (!destination.IsValid())
				throw new ArgumentNullException(nameof(destination));
			if (source.OverlapWith(destination))
				throw new InvalidOperationException(StorageError.CannotCopyOverlap);
			if (strideSource < 1)
				throw new ArgumentOutOfRangeException(nameof(strideSource));
			if (strideDestination < 1)
				throw new ArgumentOutOfRangeException(nameof(strideDestination));
			if (strideSource == 1 && strideDestination == 1)
			{
				return source.CopyTo<T, TS1, TS2>(destination);
			}

			RuntimeTypeHandle handle1 = typeof(TS1).TypeHandle, handle2 = typeof(TS2).TypeHandle;
			if (copyStridedFunc.TryGetValue((handle1, handle2), out var copier))
				goto FINAL;
			copier = GetCopyStridedMethod<TS1, TS2>();
			copyStridedFunc[(handle1, handle2)] = copier;
		FINAL:
			((Action<TS1, TS2, long, long>)copier).Invoke(source, destination, strideSource, strideDestination);
			return Math.Max((source.Length - 1) / strideSource + 1, (destination.Length - 1) / strideDestination + 1);
		}

		/// <summary>
		/// Copy the <paramref name="source"/> storage to <paramref name="destination"/> span with given strides.<br/>
		/// <c><paramref name="destination"/>[j] = <paramref name="source"/>[k] for i = 0, ..., n - 1; k = i * <paramref name="strideSource"/>, j = i * <paramref name="strideDestination"/></c>.<br/>
		/// The number of elements copied is calculated to the maximum possible value that does not exceeds the boundaries.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS">The storage class that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="source">The source storage to copy from</param>
		/// <param name="strideSource">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="source"/></param>
		/// <param name="destination">The destination span to copy to</param>
		/// <param name="strideDestination">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="destination"/></param>
		/// <returns>The number of elements (in <typeparamref name="T"/>) actually copied.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideSource"/> or <paramref name="strideDestination"/> ≤ 0</exception>
		public static unsafe long ToManagedStride<T, TS>(this TS source, long strideSource, Span<T> destination, long strideDestination = 1) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
		{
			fixed (T* dst = destination)
			{
				ManagedPointer mp = new(new(dst), destination.Length * sizeof(T));
				ManagedPureStorage<T> ms = new(mp);
				return StridedCopyTo<T, TS, ManagedPureStorage<T>>(source, strideSource, ms, strideDestination);
			}
		}

		/// <summary>
		/// Copy the <paramref name="source"/> span to <paramref name="destination"/> storage with given strides.<br/>
		/// <c><paramref name="destination"/>[j] = <paramref name="source"/>[k] for i = 0, ..., n - 1; k = i * <paramref name="strideSource"/>, j = i * <paramref name="strideDestination"/></c>.<br/>
		/// The number of elements copied is calculated to the maximum possible value that does not exceeds the boundaries.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TS">The storage class that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="source">The source span to copy from</param>
		/// <param name="strideSource">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="source"/></param>
		/// <param name="destination">The destination storage to copy to</param>
		/// <param name="strideDestination">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="destination"/></param>
		/// <returns>The number of elements (in <typeparamref name="T"/>) actually copied.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="strideSource"/> or <paramref name="strideDestination"/> ≤ 0</exception>
		public static unsafe long FromManagedStride<T, TS>(this Span<T> source, long strideSource, TS destination, long strideDestination) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
		{
			fixed (T* src = source)
			{
				ManagedPointer mp = new(new(src), source.Length * sizeof(T));
				ManagedPureStorage<T> ms = new(mp);
				return StridedCopyTo<T, ManagedPureStorage<T>, TS>(ms, strideSource, destination, strideDestination);
			}
		}
		#endregion
	}
}
