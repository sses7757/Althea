using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using Althea.Helpers;
using Althea.Linq;
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
		/// <c><paramref name="destination"/>[j] = <paramref name="source"/>[k] for i = 0, ..., n - 1; k = i * <paramref name="incrementSource"/>, j = i * <paramref name="incrementDestination"/></c>.<br/>
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
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="incrementSource"/> or <paramref name="incrementDestination"/> is less than 1</exception>
		[AbstractApiMethod]
		public abstract bool StridedCopy<T, TP1, TP2>(PointerSegment<TP1> source, int incrementSource, PointerSegment<TP2> destination, int incrementDestination, out long actualCopied) where T : unmanaged, INumber<T> where TP1 : IPointer<TP1> where TP2 : IPointer<TP2>;
		#endregion

		#region storage and managed operations
		/// <summary>
		/// When implemented by a derived class, copy out the <b>first</b> element in unmanaged pointer <paramref name="source"/> to a managed value of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TP">The pointer type that implements <see cref="IPointer{TSelf}"/></typeparam>
		/// <param name="source">The source pointer to copy from</param>
		/// <param name="value">Output the first element in <paramref name="source"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is invalid</exception>
		[AbstractApiMethod]
		public unsafe virtual bool ToManaged<T, TP>(PointerSegment<TP> source, out T value) where T : unmanaged, INumber<T> where TP : IPointer<TP>
		{
			value = default;
			ManagedPointer mp = new(new(Unsafe.AsPointer(ref value)), sizeof(T));
			return this.MemoryCopy<TP, ManagedPointer>(source, mp, out _);
		}

		/// <summary>
		/// When implemented by a derived class, overwrite the <b>first</b> element in unmanaged pointer <paramref name="destination"/> by a managed <paramref name="value"/> of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TP">The pointer type that implements <see cref="IPointer{TSelf}"/></typeparam>
		/// <param name="destination">The destination pointer to copy to</param>
		/// <param name="value">The value of type <typeparamref name="T"/> to copy from</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="destination"/> is invalid</exception>
		[AbstractApiMethod]
		public unsafe virtual bool FromManaged<T, TP>(PointerSegment<TP> destination, T value) where T : unmanaged, INumber<T> where TP : IPointer<TP>
		{
			ManagedPointer mp = new(new(&value), sizeof(T));
			return this.MemoryCopy<ManagedPointer, TP>(mp, destination, out _);
		}

		/// <summary>
		/// When implemented by a derived class, copy out the first few elements in unmanaged pointer <paramref name="source"/> to a managed array of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TP">The pointer type that implements <see cref="IPointer{TSelf}"/></typeparam>
		/// <param name="source">The source pointer to copy from</param>
		/// <param name="destination">The managed <see cref="Span{T}"/> of type <typeparamref name="T"/> to copy to</param>
		/// <param name="actualCopied">Output the number of elements (in <typeparamref name="T"/>) actually copied</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is invalid</exception>
		[AbstractApiMethod]
		public unsafe virtual bool ToManaged<T, TP>(PointerSegment<TP> source, Span<T> destination, out long actualCopied) where T : unmanaged, INumber<T> where TP : IPointer<TP>
		{
			if (destination.IsEmpty)
				throw new ArgumentNullException(nameof(destination));
			fixed (T* dst = destination)
			{
				ManagedPointer mp = new(new(dst), sizeof(T) * destination.Length);
				return this.MemoryCopy<TP, ManagedPointer>(source, mp, out actualCopied);
			}
		}

		/// <summary>
		/// When implemented by a derived class, overwrite the first few elements in unmanaged pointer <paramref name="destination"/> by the <paramref name="values"/> of a managed array of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TP">The pointer type that implements <see cref="IPointer{TSelf}"/></typeparam>
		/// <param name="destination">The destination pointer to copy to</param>
		/// <param name="values">The managed <see cref="ReadOnlySpan{T}"/> of type <typeparamref name="T"/> to copy from</param>
		/// <param name="actualCopied">Output the number of elements (in <typeparamref name="T"/>) actually copied</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="destination"/> is invalid</exception>
		[AbstractApiMethod]
		public unsafe virtual bool FromManaged<T, TP>(PointerSegment<TP> destination, ReadOnlySpan<T> values, out long actualCopied) where T : unmanaged, INumber<T> where TP : IPointer<TP>
		{
			if (values.IsEmpty)
				throw new ArgumentNullException(nameof(values));
			fixed (T* src = values)
			{
				ManagedPointer mp = new(new(src), sizeof(T) * values.Length);
				return this.MemoryCopy<ManagedPointer, TP>(mp, destination, out actualCopied);
			}
		}

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
		public unsafe virtual bool ToManaged2D<T, TP>(PointerSegment<TP> source, long leadDim, long height, long width, Span<T> destination, long destinationLeadDim = 0) where T : unmanaged, INumber<T> where TP : IPointer<TP>
		{
			if (destination.IsEmpty)
				throw new ArgumentNullException(nameof(destination));
			if (destinationLeadDim == 0)
				destinationLeadDim = height;
			leadDim *= sizeof(T); height *= sizeof(T); width *= sizeof(T); destinationLeadDim *= sizeof(T);
			fixed (T* dst = destination)
			{
				ManagedPointer mp = new(new(dst), sizeof(T) * destination.Length);
				return this.MemoryCopy2D<TP, ManagedPointer>(source, leadDim, mp, destinationLeadDim, height, width);
			}
		}

		/// <summary>
		/// When implemented by a derived class, overwrite some of the elements in unmanaged pointer <paramref name="destination"/> as a 2D matrix by a managed array of type <typeparamref name="T"/> (viewed as a 1D array).
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
		public unsafe virtual bool FromManaged2D<T, TP>(PointerSegment<TP> destination, long leadDim, long height, long width, ReadOnlySpan<T> values, long valuesLeadDim = 0) where T : unmanaged, INumber<T> where TP : IPointer<TP>
		{
			if (values.IsEmpty)
				throw new ArgumentNullException(nameof(values));
			if (valuesLeadDim == 0)
				valuesLeadDim = height;
			leadDim *= sizeof(T); height *= sizeof(T); width *= sizeof(T); valuesLeadDim *= sizeof(T);
			fixed (T* src = values)
			{
				ManagedPointer mp = new(new(src), sizeof(T) * values.Length);
				return this.MemoryCopy2D<ManagedPointer, TP>(mp, valuesLeadDim, destination, leadDim, height, width);
			}
		}

		/// <summary>
		/// When implemented by a derived class, copy the <paramref name="source"/> storage to <paramref name="destination"/> which is a managed array of type <typeparamref name="T"/> with given strides.<br/>
		/// <c><paramref name="destination"/>[j] = <paramref name="source"/>[k] for i = 0, ..., n - 1; k = i * <paramref name="incrementSource"/>, j = i * <paramref name="incrementDestination"/></c>.<br/>
		/// The number of elements copied is calculated to the maximum possible value that does not exceeds the boundaries.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TP">A pointer type that implements <see cref="IPointer{TSelf}"/></typeparam>
		/// <param name="source">The source pointer to copy from</param>
		/// <param name="incrementSource">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="source"/></param>
		/// <param name="destination">The destination managed array to copy to</param>
		/// <param name="incrementDestination">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="destination"/></param>
		/// <param name="actualCopied">Output the number of elements (in <typeparamref name="T"/>) actually copied</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="incrementSource"/> or <paramref name="incrementDestination"/> is less than 1</exception>
		[AbstractApiMethod]
		public unsafe virtual bool ToManagedStrided<T, TP>(PointerSegment<TP> source, int incrementSource, Span<T> destination, int incrementDestination, out long actualCopied) where T : unmanaged, INumber<T> where TP : IPointer<TP>
		{
			if (destination.IsEmpty)
				throw new ArgumentNullException(nameof(destination));
			fixed (T* dst = destination)
			{
				ManagedPointer mp = new(new(dst), sizeof(T) * destination.Length);
				return this.StridedCopy<T, TP, ManagedPointer>(source, incrementSource, mp, incrementDestination, out actualCopied);
			}
		}

		/// <summary>
		/// When implemented by a derived class, copy some of the values in the <paramref name="values"/> managed array of type <typeparamref name="T"/> to <paramref name="destination"/> storage with given strides.<br/>
		/// <c><paramref name="destination"/>[j] = <paramref name="values"/>[k] for i = 0, ..., n - 1; k = i * <paramref name="incrementValues"/>, j = i * <paramref name="incrementDestination"/></c>.<br/>
		/// The number of elements copied is calculated to the maximum possible value that does not exceeds the boundaries.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number struct as the data type</typeparam>
		/// <typeparam name="TP">A pointer type that implements <see cref="IPointer{TSelf}"/></typeparam>
		/// <param name="values">The source managed array to copy from</param>
		/// <param name="incrementValues">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="values"/></param>
		/// <param name="destination">The destination storage to copy to</param>
		/// <param name="incrementDestination">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="destination"/></param>
		/// <param name="actualCopied">Output the number of elements (in <typeparamref name="T"/>) actually copied</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="values"/> or <paramref name="destination"/> is invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="incrementValues"/> or <paramref name="incrementDestination"/> is less than 1</exception>
		[AbstractApiMethod]
		public unsafe virtual bool FromManagedStrided<T, TP>(PointerSegment<TP> destination, int incrementDestination, Span<T> values, int incrementValues, out long actualCopied) where T : unmanaged, INumber<T> where TP : IPointer<TP>
		{
			if (values.IsEmpty)
				throw new ArgumentNullException(nameof(values));
			fixed (T* src = values)
			{
				ManagedPointer mp = new(new(src), sizeof(T) * values.Length);
				return this.StridedCopy<T, ManagedPointer, TP>(mp, incrementValues, destination, incrementDestination, out actualCopied);
			}
		}
		#endregion
	}


	/// <summary>
	/// The static class for extension methods for <see cref="IStorage"/> and <see cref="IStorage{T, TSelf}"/>
	/// </summary>
	public static class StorageExtension
	{
		#region method generators
#pragma warning disable CS8601
		private static readonly MethodInfo getSizeOfPointer = typeof(IStorage).GetMethod(nameof(IStorage.SizeOfPointer), 0, BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(int) }, null);
#pragma warning restore CS8601

		private static Action<TS, T> GetFillMethod<TS, T>() where TS : class, IStorage where T : unmanaged, INumber<T>
		{
			var type = typeof(TS);
			var pointerGetters = TS.PointerGetters;
			var pointerFill = new MethodInfo[pointerGetters.Length];
			for (int i = 0; i < pointerGetters.Length; i++)
			{
				var pg = pointerGetters[i];
				if (pg is null || pg.ReturnType.GenericTypeArguments.Length != 1)
					throw new InvalidOperationException(StorageException.InvalidPointerGetter);
				if (pg.GetParameters().Length != 0 &&
					!(pg.GetParameters().Length == 2 && pg.GetParameters()
														  .Select(static p => p.ParameterType)
														  .SequenceEqual(new[] { typeof(long), typeof(bool) })))
					throw new InvalidOperationException(StorageException.InvalidPointerGetter);
				var fillMethod = typeof(ApiSelector).GetMethod(nameof(ApiSelector.FillWithValue), typeof(T) == typeof(byte) ? 1 : 2, BindingFlags.Public | BindingFlags.Static, null, new[] { pg.ReturnType, typeof(T) }, null)?.MakeGenericMethod(pg.ReturnType.GenericTypeArguments[0]);
				if (fillMethod is null)
					throw new InvalidOperationException(StorageException.InvalidPointerGetter);
				pointerFill[i] = fillMethod;
			}

			DynamicMethod method = new($"{typeof(T).GetGenericString()} filler of {type.GetGenericString()}", null, new[] { type, typeof(T) });
			var IL = method.GetILGenerator();
			(Label end, Label other)[] labels = new (Label, Label)[pointerGetters.Length];
			for (int i = 0; i < pointerGetters.Length; i++)
			{
				IL.Emit(OpCodes.Ldarg_0);
				IL.Emit(OpCodes.Ldc_I4, i);
				IL.Emit(OpCodes.Callvirt, getSizeOfPointer);
				IL.DeclareLocal(typeof(long));
				IL.Emit(OpCodes.Stloc_S, i); // long sizePointerI = storage.SizeOfPointer(i);
				labels[i] = (IL.DefineLabel(), IL.DefineLabel());
			}
			IL.DeclareLocal(typeof(long)); // local long i
			for (int i = 0; i < pointerGetters.Length; i++)
			{
				IL.Emit(OpCodes.Ldloc_S, i);
				IL.Emit(OpCodes.Brfalse_S, labels[i].end); // if (sizePointerI == 0) goto POINTER_I_END;
				if (pointerGetters[i].GetParameters().Length == 0)
				{
					IL.Emit(OpCodes.Ldloc_S, i);
					IL.Emit(OpCodes.Ldc_I4_1);
					IL.Emit(OpCodes.Bne_Un_S, labels[i].other); // if (sizePointerI != 1) goto POINTER_I_ERROR;
					IL.Emit(OpCodes.Ldarg_0);
					IL.Emit(OpCodes.Callvirt, pointerGetters[i]);
					IL.Emit(OpCodes.Ldarg_1);
					IL.Emit(OpCodes.Call, pointerFill[i]); // ApiSelector.FillWithValue(storage.PointerI, value);
					IL.MarkLabel(labels[i].other);
					IL.ThrowException(typeof(InvalidOperationException)); // POINTER_I_ERROR: throw new InvalidOperationException();
				}
				else
				{
					IL.Emit(OpCodes.Ldc_I8, 0L);
					IL.Emit(OpCodes.Stloc_S, pointerGetters.Length); // long i = 0;
					IL.MarkLabel(labels[i].other); // LOOP_I:
					IL.Emit(OpCodes.Ldarg_0);
					IL.Emit(OpCodes.Ldloc_S, pointerGetters.Length);
					IL.Emit(OpCodes.Ldc_I4_1);
					IL.Emit(OpCodes.Callvirt, pointerGetters[i]);
					IL.Emit(OpCodes.Ldarg_1);
					IL.Emit(OpCodes.Call, pointerFill[i]); // ApiSelector.FillWithValue(storage.PointerI(i, true), value);
					IL.Emit(OpCodes.Ldloc_S, pointerGetters.Length);
					IL.Emit(OpCodes.Ldc_I8, 1L);
					IL.Emit(OpCodes.Add);
					IL.Emit(OpCodes.Stloc_S, pointerGetters.Length);
					IL.Emit(OpCodes.Ldloc_S, pointerGetters.Length);
					IL.Emit(OpCodes.Ldloc_S, i); // if (++i < sizePointerI) goto LOOP_I;
					IL.Emit(OpCodes.Blt_S, labels[i].other);
				}
				IL.MarkLabel(labels[i].end); // POINTER_I_END:
			}

			method.DefineParameter(1, ParameterAttributes.In, "storage");
			method.DefineParameter(2, ParameterAttributes.In, "value");
			return method.CreateDelegate<Action<TS, T>>();
		}

		private static Action<TS1, TS2> GetCopyMethod<TS1, TS2>() where TS1 : class, IStorage where TS2 : class, IStorage
		{
			Type type1 = typeof(TS1), type2 = typeof(TS2);
			MethodInfo[] pointerGetters1 = TS2.PointerGetters, pointerGetters2 = TS2.PointerGetters;
			MethodInfo[] pointerLen1 = new MethodInfo[pointerGetters1.Length], pointerMove1 = new MethodInfo[pointerGetters1.Length];
			MethodInfo[] pointerLen2 = new MethodInfo[pointerGetters2.Length], pointerMove2 = new MethodInfo[pointerGetters2.Length];
			MethodInfo[,] pointerCopy = new MethodInfo[pointerGetters1.Length, pointerGetters2.Length];
			for (int i = 0; i < pointerGetters1.Length; i++)
			{
				var pg = pointerGetters1[i];
				bool first = true;
			SET_METHOD:
				if (pg is null || pg.ReturnType.GenericTypeArguments.Length != 1)
					throw new InvalidOperationException(StorageException.InvalidPointerGetter);
				if (pg.GetParameters().Length != 0 &&
					!(pg.GetParameters().Length == 2 && pg.GetParameters()
														  .Select(static p => p.ParameterType)
														  .SequenceEqual(new[] { typeof(long), typeof(bool) })))
					throw new InvalidOperationException(StorageException.InvalidPointerGetter);
				var ptrLen = pg.ReturnType.GetProperty(nameof(PointerSegment<ManagedPointer>.LengthInBytes), BindingFlags.Public | BindingFlags.Instance)?.GetAccessors(false)?.FirstOrDefault();
				if (ptrLen is null || ptrLen.ReturnType != typeof(long))
					throw new InvalidOperationException(StorageException.InvalidPointerGetter);
				var ptrMove = pg.ReturnType.GetMethod(nameof(PointerSegment<ManagedPointer>.MoveBy), 0, BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(long), typeof(long) }, null);
				if (ptrMove is null)
					throw new InvalidOperationException(StorageException.InvalidPointerGetter);
				if (first)
				{
					pointerLen1[i] = ptrLen; pointerMove1[i] = ptrMove;
					pg = pointerGetters2[i];
					first = false;
					goto SET_METHOD;
				}
				pointerLen2[i] = ptrLen; pointerMove2[i] = ptrMove;
			}
			for (int i = 0; i < pointerGetters1.Length; i++)
			{
				for (int j = 0; j < pointerGetters2.Length; j++)
				{
					var copyMethod = typeof(ApiSelector).GetMethod(nameof(ApiSelector.MemoryCopy), 2, BindingFlags.Public | BindingFlags.Static, null, new[] { pointerGetters1[i].ReturnType, pointerGetters2[j].ReturnType }, null)?.MakeGenericMethod(pointerGetters1[i].ReturnType.GenericTypeArguments[0], pointerGetters2[j].ReturnType.GenericTypeArguments[0]);
					if (copyMethod is null)
						throw new InvalidOperationException(StorageException.InvalidPointerGetter);
					pointerCopy[i, j] = copyMethod;
				}
			}

			DynamicMethod method = new($"Copier from {type1.GetGenericString()} to {type2.GetGenericString()}", null, new[] { type1, type2 });
			var IL = method.GetILGenerator();
			Label ret = IL.DefineLabel(), thr = IL.DefineLabel();
			Label[,] branches = new Label[pointerGetters1.Length + 1, pointerGetters2.Length + 1];
			for (int i = 0; i <= pointerGetters1.Length; i++)
				for (int j = 0; j <= pointerGetters2.Length; j++)
					branches[i, j] = i == pointerGetters1.Length || j == pointerGetters2.Length ? ret : IL.DefineLabel();
			for (int i = 0; i < pointerGetters1.Length; i++)
			{
				IL.Emit(OpCodes.Ldarg_0);
				IL.Emit(OpCodes.Ldc_I4, i);
				IL.Emit(OpCodes.Callvirt, getSizeOfPointer);
				IL.DeclareLocal(typeof(long));
				IL.Emit(OpCodes.Stloc_S, i); // long sizeSrcPointerI = src.SizeOfPointer(i);
			}
			for (int j = 0; j < pointerGetters2.Length; j++)
			{
				IL.Emit(OpCodes.Ldarg_1);
				IL.Emit(OpCodes.Ldc_I4, j);
				IL.Emit(OpCodes.Callvirt, getSizeOfPointer);
				IL.DeclareLocal(typeof(long));
				IL.Emit(OpCodes.Stloc_S, j + pointerGetters1.Length); // long sizeDstPointerJ = dst.SizeOfPointer(i);
			}
			for (int i = 0; i < pointerGetters1.Length; i++)
			{
				IL.DeclareLocal(pointerGetters1[i].ReturnType);
			}
			for (int j = 0; j < pointerGetters2.Length; j++)
			{
				IL.DeclareLocal(pointerGetters2[j].ReturnType);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			int sizeSrcPointer(int i) => i;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			int sizeDstPointer(int j) => j + pointerGetters1.Length;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			int srcPointer(int i) => i + pointerGetters1.Length + pointerGetters2.Length;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			int dstPointer(int j) => j + pointerGetters1.Length * 2 + pointerGetters2.Length;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			int copied() => (pointerGetters1.Length + pointerGetters2.Length) * 2;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			int offsetSrc() => (pointerGetters1.Length + pointerGetters2.Length) * 2 + 1;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			int offsetDst() => (pointerGetters1.Length + pointerGetters2.Length) * 2 + 2;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			int loopI() => (pointerGetters1.Length + pointerGetters2.Length) * 2 + 3;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			int loopJ() => (pointerGetters1.Length + pointerGetters2.Length) * 2 + 4;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			void LocalIInc(bool save = true)
			{
				IL.Emit(OpCodes.Ldloc_S, loopI());
				IL.Emit(OpCodes.Ldc_I8, 1L);
				IL.Emit(OpCodes.Add);
				if (save)
					IL.Emit(OpCodes.Dup);
				IL.Emit(OpCodes.Stloc_S, loopI());
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			void LocalJInc(bool save = true)
			{
				IL.Emit(OpCodes.Ldloc_S, loopJ());
				IL.Emit(OpCodes.Ldc_I8, 1L);
				IL.Emit(OpCodes.Add);
				if (save)
					IL.Emit(OpCodes.Dup);
				IL.Emit(OpCodes.Stloc_S, loopJ());
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			void LocalSrcNext(int i)
			{
				IL.Emit(OpCodes.Ldarg_0);
				IL.Emit(OpCodes.Ldloc_S, loopI());
				IL.Emit(OpCodes.Ldc_I4_0);
				IL.Emit(OpCodes.Callvirt, pointerGetters1[i]);
				IL.Emit(OpCodes.Stloc_S, srcPointer(i)); // srcPtrI = src.PointerI(i, false);
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			void LocalDstNext(int j)
			{
				IL.Emit(OpCodes.Ldarg_1);
				IL.Emit(OpCodes.Ldloc_S, loopJ());
				IL.Emit(OpCodes.Ldc_I4_1);
				IL.Emit(OpCodes.Callvirt, pointerGetters2[j]);
				IL.Emit(OpCodes.Stloc_S, dstPointer(j)); // dstPtrI = dst.PointerI(j, true);
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			void LocalSrcMove(int i)
			{
				IL.Emit(OpCodes.Ldloc_S, srcPointer(i));
				IL.Emit(OpCodes.Ldloc_S, copied());
				IL.Emit(OpCodes.Ldc_I8, 0L);
				IL.Emit(OpCodes.Calli, pointerMove1[i]);
				IL.Emit(OpCodes.Stloc_S, srcPointer(i)); // srcPtrI = srcPtrI.Move(copied);
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			void LocalDstMove(int j)
			{
				IL.Emit(OpCodes.Ldloc_S, dstPointer(j));
				IL.Emit(OpCodes.Ldloc_S, copied());
				IL.Emit(OpCodes.Ldc_I8, 0L);
				IL.Emit(OpCodes.Calli, pointerMove2[j]);
				IL.Emit(OpCodes.Stloc_S, srcPointer(j)); // dstPtrJ = dstPtrJ.Move(copied);
			}

			IL.Emit(OpCodes.Ldc_I8, 0L);
			IL.DeclareLocal(typeof(long));
			IL.Emit(OpCodes.Stloc_S, copied()); // long copied = 0;
			IL.Emit(OpCodes.Ldc_I8, 0L);
			IL.DeclareLocal(typeof(long));
			IL.Emit(OpCodes.Stloc_S, offsetSrc()); // long offsetSrc = 0;
			IL.Emit(OpCodes.Ldc_I8, 0L);
			IL.DeclareLocal(typeof(long));
			IL.Emit(OpCodes.Stloc_S, offsetDst()); // long offsetDst = 0;
			IL.Emit(OpCodes.Ldc_I8, 0L);
			IL.DeclareLocal(typeof(long));
			IL.Emit(OpCodes.Stloc_S, loopI()); // long i = 0;
			IL.Emit(OpCodes.Ldc_I8, 0L);
			IL.DeclareLocal(typeof(long));
			IL.Emit(OpCodes.Stloc_S, loopJ()); // long j = 0;

			for (int i = 0; i < pointerGetters1.Length; i++)
			{
				for (int j = 0; j < pointerGetters2.Length; j++)
				{
					IL.MarkLabel(branches[i, j]);
					int type = (pointerGetters1[i].GetParameters().Length != 0 ? 0.SetBit(0) : 0) + (pointerGetters2[j].GetParameters().Length != 0 ? 0.SetBit(1) : 0);
					IL.Emit(OpCodes.Ldloc_S, sizeSrcPointer(i));
					IL.Emit(OpCodes.Brfalse_S, branches[i + 1, j]); // if (sizeSrcPointerI == 0) goto P[I+1, J];
					if (type.IsBitNotSet(0))
					{	// if (sizeSrcPointerI != 1) throw;
						IL.Emit(OpCodes.Ldloc_S, sizeSrcPointer(i));
						IL.Emit(OpCodes.Ldc_I8, 1L);
						IL.Emit(OpCodes.Bne_Un_S, thr);
					}
					IL.Emit(OpCodes.Ldloc_S, sizeDstPointer(j));
					IL.Emit(OpCodes.Brfalse_S, branches[i, j + 1]); // if (sizeSrcPointerJ == 0) goto P[I, J+1];
					if (type.IsBitNotSet(1))
					{   // if (sizeSrcPointerJ != 1) throw;
						IL.Emit(OpCodes.Ldloc_S, sizeDstPointer(j));
						IL.Emit(OpCodes.Ldc_I8, 1L);
						IL.Emit(OpCodes.Bne_Un_S, thr);
					}

					IL.Emit(OpCodes.Ldarg_0);
					if (type.IsBitNotSet(0))
					{
						IL.Emit(OpCodes.Callvirt, pointerGetters1[i]);
					}
					else
					{
						IL.Emit(OpCodes.Ldloc_S, loopI());
						IL.Emit(OpCodes.Ldc_I4_0);
						IL.Emit(OpCodes.Callvirt, pointerGetters1[i]);
					}
					IL.Emit(OpCodes.Ldloc_S, offsetSrc());
					IL.Emit(OpCodes.Ldc_I8, 0L);
					IL.Emit(OpCodes.Calli, pointerMove1[i]);
					IL.Emit(OpCodes.Stloc_S, srcPointer(i)); // srcPtrI = src.PointerI(i, false).Move(offsetSrc);
					IL.Emit(OpCodes.Ldarg_1);
					if (type.IsBitNotSet(1))
					{
						IL.Emit(OpCodes.Callvirt, pointerGetters2[j]);
					}
					else
					{
						IL.Emit(OpCodes.Ldloc_S, loopJ());
						IL.Emit(OpCodes.Ldc_I4_1);
						IL.Emit(OpCodes.Callvirt, pointerGetters2[j]);
					}
					IL.Emit(OpCodes.Ldloc_S, offsetDst());
					IL.Emit(OpCodes.Ldc_I8, 0L);
					IL.Emit(OpCodes.Calli, pointerMove2[j]);
					IL.Emit(OpCodes.Stloc_S, dstPointer(j)); // dstPtrJ = dst.PointerJ(j, true).Move(offsetDst);

					Label srcMove = IL.DefineLabel(), dstMove = IL.DefineLabel();
					Label loopStart = default, c1next = default, c2next = default, c3next = default, c1next1 = default, c1next2 = default;
					if (type != 0)
					{
						loopStart = IL.DefineLabel(); c1next = IL.DefineLabel(); c2next = IL.DefineLabel(); c3next = IL.DefineLabel();
						IL.MarkLabel(loopStart);
					}

					// while (true) {
					IL.Emit(OpCodes.Ldloc_S, srcPointer(i));
					IL.Emit(OpCodes.Ldloc_S, dstPointer(j));
					IL.Emit(OpCodes.Call, pointerCopy[i, j]);
					IL.Emit(OpCodes.Dup);
					IL.Emit(OpCodes.Stloc_S, copied()); // copied = ApiSelector.MemoryCopy(srcPtrI, dstPtrJ);
					IL.Emit(OpCodes.Ldloc_S, srcPointer(i));
					IL.Emit(OpCodes.Calli, pointerLen1[i]);
					IL.Emit(OpCodes.Bne_Un_S, srcMove);
					IL.Emit(OpCodes.Ldloc_S, copied());
					IL.Emit(OpCodes.Ldloc_S, dstPointer(j));
					IL.Emit(OpCodes.Calli, pointerLen2[j]);
					IL.Emit(OpCodes.Bne_Un_S, srcMove);
					// if (copied == srcPtrI.LengthInBytes && copied == dstPtrJ.LengthInBytes) {
					IL.Emit(OpCodes.Ldc_I8, 0L);
					IL.Emit(OpCodes.Stloc_S, offsetSrc());
					IL.Emit(OpCodes.Ldc_I8, 0L);
					IL.Emit(OpCodes.Stloc_S, offsetDst()); // offsetSrc = offsetDst = 0;
					switch (type)
					{
						case 0b00:
							IL.Emit(OpCodes.Br_S, branches[i + 1, j + 1]); // goto P[I+1, J+1];
							break;
						case 0b01:
							LocalIInc();
							IL.Emit(OpCodes.Ldloc_S, sizeSrcPointer(i));
							IL.Emit(OpCodes.Blt_S, c1next); // if (++i >= sizeSrcPointerI) {
							IL.Emit(OpCodes.Ldc_I8, 0L);
							IL.Emit(OpCodes.Stloc_S, loopI()); // i = 0;
							IL.Emit(OpCodes.Br_S, branches[i + 1, j + 1]);i = 0; // goto P[I+1, J+1]; }
							IL.MarkLabel(c1next);
							IL.Emit(OpCodes.Br_S, branches[i, j + 1]); // goto P[I, J+1];
							break;
						case 0b10:
							LocalJInc();
							IL.Emit(OpCodes.Ldloc_S, sizeDstPointer(j));
							IL.Emit(OpCodes.Blt_S, c1next); // if (++j >= sizeDstPointerJ) {
							IL.Emit(OpCodes.Ldc_I8, 0L);
							IL.Emit(OpCodes.Stloc_S, loopJ()); // j = 0;
							IL.Emit(OpCodes.Br_S, branches[i + 1, j + 1]); // goto P[I+1, J+1]; }
							IL.MarkLabel(c1next);
							IL.Emit(OpCodes.Br_S, branches[i + 1, j]); // goto P[I+1, J];
							break;
						case 0b11:
							c1next1 = IL.DefineLabel(); c1next2 = IL.DefineLabel();
							LocalIInc(false);
							LocalJInc(false);
							IL.Emit(OpCodes.Ldloc_S, loopI());
							IL.Emit(OpCodes.Ldloc_S, sizeSrcPointer(i));
							IL.Emit(OpCodes.Bge_S, c1next);
							IL.Emit(OpCodes.Ldloc_S, loopJ());
							IL.Emit(OpCodes.Ldloc_S, sizeDstPointer(j));
							IL.Emit(OpCodes.Bge_S, c1next); // if (++i < sizeSrcPointerI & ++j < sizeDstPointerJ) {
							LocalSrcNext(i);
							LocalDstNext(j);
							IL.Emit(OpCodes.Br_S, loopStart); // continue; }
							IL.MarkLabel(c1next);
							IL.Emit(OpCodes.Ldloc_S, loopI());
							IL.Emit(OpCodes.Ldloc_S, sizeSrcPointer(i));
							IL.Emit(OpCodes.Bge_S, c1next1); // else if (i < sizeSrcPointerI) {
							IL.Emit(OpCodes.Ldc_I8, 0L);
							IL.Emit(OpCodes.Stloc_S, loopJ()); // j = 0;
							IL.Emit(OpCodes.Br_S, branches[i, j + 1]); // goto P[I, J+1]; }
							IL.MarkLabel(c1next1);
							IL.Emit(OpCodes.Ldloc_S, loopJ());
							IL.Emit(OpCodes.Ldloc_S, sizeDstPointer(j));
							IL.Emit(OpCodes.Blt_S, c1next2); // else if (j < sizeDstPointerJ) {
							IL.Emit(OpCodes.Ldc_I8, 0L);
							IL.Emit(OpCodes.Stloc_S, loopI()); // i = 0;
							IL.Emit(OpCodes.Br_S, branches[i + 1, j]); // goto P[I+1, J]; }
							IL.MarkLabel(c1next2); // else {
							IL.Emit(OpCodes.Ldc_I8, 0L);
							IL.Emit(OpCodes.Stloc_S, loopI());
							IL.Emit(OpCodes.Ldc_I8, 0L);
							IL.Emit(OpCodes.Stloc_S, loopJ()); // i = j = 0;
							IL.Emit(OpCodes.Br_S, branches[i + 1, j + 1]); // goto P[I+1, J+1]; }
							break;
					}
					// }
					// else if (copied < srcPtrI.LengthInBytes) {
					IL.MarkLabel(srcMove);
					IL.Emit(OpCodes.Ldloc_S, copied());
					IL.Emit(OpCodes.Ldloc_S, srcPointer(i));
					IL.Emit(OpCodes.Calli, pointerLen1[i]);
					IL.Emit(OpCodes.Blt_S, dstMove);
					IL.Emit(OpCodes.Ldloc_S, offsetSrc());
					IL.Emit(OpCodes.Ldloc_S, copied());
					IL.Emit(OpCodes.Add);
					IL.Emit(OpCodes.Stloc_S, offsetSrc()); // offsetSrc += copied;
					IL.Emit(OpCodes.Ldc_I8, 0L);
					IL.Emit(OpCodes.Stloc_S, offsetDst()); // offsetDst = 0; 
					if (type.IsBitNotSet(1))
					{
						IL.Emit(OpCodes.Br_S, branches[i, j + 1]); // goto P[I, J+1];
					}
					else
					{
						LocalSrcMove(i);
						LocalJInc();
						IL.Emit(OpCodes.Ldloc_S, sizeDstPointer(j));
						IL.Emit(OpCodes.Blt_S, c2next); // if (++j >= sizeDstPointerJ) {
						IL.Emit(OpCodes.Ldc_I8, 0L);
						IL.Emit(OpCodes.Stloc_S, loopJ()); // j = 0;
						IL.Emit(OpCodes.Br_S, branches[i, j + 1]); // goto P[I, J+1]; }
						IL.MarkLabel(c2next);
						LocalDstNext(j);
						IL.Emit(OpCodes.Br_S, loopStart); // continue;
					}
					// }
					// else if (copied < dstPtrJ.LengthInBytes) {
					IL.MarkLabel(dstMove);
					IL.Emit(OpCodes.Ldloc_S, offsetDst());
					IL.Emit(OpCodes.Ldloc_S, copied());
					IL.Emit(OpCodes.Add);
					IL.Emit(OpCodes.Stloc_S, offsetDst()); // offsetDst += copied;
					IL.Emit(OpCodes.Ldc_I8, 0L);
					IL.Emit(OpCodes.Stloc_S, offsetSrc()); // offsetSrc = 0;
					if (type.IsBitNotSet(0))
					{
						IL.Emit(OpCodes.Br_S, branches[i + 1, j]); // goto P[I+1, J];
					}
					else
					{
						LocalDstMove(j);
						LocalIInc();
						IL.Emit(OpCodes.Ldloc_S, sizeSrcPointer(i));
						IL.Emit(OpCodes.Blt_S, c3next); // if (++i >= sizeSrcPointerI) {
						IL.Emit(OpCodes.Ldc_I8, 0L);
						IL.Emit(OpCodes.Stloc_S, loopI()); // i = 0;
						IL.Emit(OpCodes.Br_S, branches[i + 1, j]); // goto P[I+1, J]; }
						IL.MarkLabel(c3next);
						LocalSrcNext(i);
						IL.Emit(OpCodes.Br_S, loopStart); // continue;
					}
					// }
					// } end while
				}
			}

			IL.MarkLabel(ret);
			IL.Emit(OpCodes.Ret);
			IL.MarkLabel(thr);
			IL.ThrowException(typeof(InvalidOperationException));

			method.DefineParameter(1, ParameterAttributes.In, "source");
			method.DefineParameter(2, ParameterAttributes.In, "destination");
			return method.CreateDelegate<Action<TS1, TS2>>();
		}
		#endregion

		#region extension methods
		private static readonly Dictionary<RuntimeTypeHandle, Delegate> fillByteFunc = new();

		/// <summary>
		/// Fill the given <paramref name="storage"/> with given <paramref name="value"/> byte by byte.
		/// </summary>
		/// <typeparam name="TS">The storage class that implements <see cref="IStorage"/></typeparam>
		/// <param name="storage">The storage to be filled with <paramref name="value"/></param>
		/// <param name="value">The byte value to fill</param>
		/// <exception cref="ObjectDisposedException">If <paramref name="storage"/> is invalid</exception>
		/// <exception cref="InvalidOperationException">If the <see cref="IStorage.PointerGetters"/> of <typeparamref name="TS"/> are not correct pointer property names</exception>
		public static void FillWith<TS>(this TS storage, byte value) where TS : class, IStorage
		{
			if (!storage.IsValid())
				throw new ObjectDisposedException(nameof(storage));

			var handle = typeof(TS).TypeHandle;
			if (fillByteFunc.TryGetValue(handle, out var filler))
				goto FINAL;
			filler = GetFillMethod<TS, byte>();
			fillByteFunc[handle] = filler;
		FINAL:
			((Action<TS, byte>)filler).Invoke(storage, value);
		}

		private static readonly Dictionary<RuntimeTypeHandle, Delegate> fillTFunc = new();

		/// <summary>
		/// Fill the given <paramref name="storage"/> with given <paramref name="value"/> <typeparamref name="T"/> by <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The data type of <typeparamref name="TS"/></typeparam>
		/// <typeparam name="TS">The storage class that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="storage">The storage to be filled with <paramref name="value"/></param>
		/// <param name="value">The byte value to fill</param>
		/// <exception cref="ObjectDisposedException">If <paramref name="storage"/> is invalid</exception>
		/// <exception cref="InvalidOperationException">If the <see cref="IStorage.PointerGetters"/> of <typeparamref name="TS"/> are not correct pointer property names</exception>
		public static unsafe void FillWith<T, TS>(this TS storage, T value) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
		{
			if (!storage.IsValid())
				throw new ObjectDisposedException(nameof(storage));
			if (sizeof(T) == sizeof(byte) || new ReadOnlySpan<byte>(&value, sizeof(T)).AllSame())
			{
				FillWith(storage, *(byte*)&value);
				return;
			}

			var handle = typeof(TS).TypeHandle;
			if (fillTFunc.TryGetValue(handle, out var filler))
				goto FINAL;
			filler = GetFillMethod<TS, T>();
			fillTFunc[handle] = filler;
		FINAL:
			((Action<TS, T>)filler).Invoke(storage, value);
		}

		private static readonly Dictionary<(RuntimeTypeHandle, RuntimeTypeHandle), Delegate> copyByteFunc = new();

		/// <summary>
		/// Copy the data from <paramref name="source"/> storage to <paramref name="destination"/> storage with copy length as the maximum of both <see cref="IStorage.LengthInBytes"/>.
		/// </summary>
		/// <typeparam name="TS1">The source storage class that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <typeparam name="TS2">The destination storage class that implements <see cref="IStorage{T, TSelf}"/></typeparam>
		/// <param name="source">The source storage</param>
		/// <param name="destination">The destination storage</param>
		/// <returns>Actual length in bytes copied.</returns>
		/// <exception cref="ObjectDisposedException">If <paramref name="source"/> or <paramref name="destination"/> is invalid</exception>
		/// <exception cref="InvalidOperationException">If <paramref name="source"/> overlaps with <paramref name="destination"/> or the <see cref="IStorage.PointerGetters"/> of <typeparamref name="TS1"/> or <typeparamref name="TS2"/> are not correct pointer property names</exception>
		public static long CopyTo<TS1, TS2>(this TS1 source, TS2 destination) where TS1 : class, IStorage where TS2 : class, IStorage
		{
			if (!source.IsValid())
				throw new ObjectDisposedException(nameof(source));
			if (!destination.IsValid())
				throw new ObjectDisposedException(nameof(destination));
			if (source.OverlapWith(destination))
				throw new InvalidOperationException(StorageException.CannotCopyOverlap);

			RuntimeTypeHandle handle1 = typeof(TS1).TypeHandle, handle2 = typeof(TS2).TypeHandle;
			if (copyByteFunc.TryGetValue((handle1, handle2), out var copier))
				goto FINAL;
			copier = GetCopyMethod<TS1, TS2>();
			copyByteFunc[(handle1, handle2)] = copier;
		FINAL:
			long copyLen = Math.Min(source.LengthInBytes, destination.LengthInBytes);
			((Action<TS1, TS2, long>)copier).Invoke(source, destination, copyLen);
			return copyLen;
		}
		#endregion
	}
}