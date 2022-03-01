using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;

using Althea.Helpers;
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
	/// The static class for extension methods for <see cref="IStorage"/> and <see cref="IStorage{T, TSelf}"/>
	/// </summary>
	public static class StorageExtension
	{
		#region method generators
		private static Action<TS, T> GetFillMethod<TS, T>(int genericCount) where TS : class, IStorage where T : unmanaged
		{
			var type = typeof(TS);
			var pointerGetters = TS.PointerNames.Select(n => type.GetProperty(n)?.GetAccessors(false)?.FirstOrDefault());
			var requestMethod = type.GetMethod(nameof(IStorage.Request), 0, BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(long), typeof(long), typeof(bool) }, null);
			DynamicMethod method = new($"Filler of {type.GetGenericString()}", null, new[] { type, typeof(T) });
			var IL = method.GetILGenerator();
			IL.Emit(OpCodes.Ldc_I8, 0L);
			IL.Emit(OpCodes.Stloc_0); // long allOffset = 0;
			foreach (var pg in pointerGetters)
			{
				if (pg is null || pg.ReturnType.GenericTypeArguments.Length != 1)
					throw new InvalidOperationException(StorageException.InvalidPointerName);
				var ptrLen = pg.ReturnType.GetProperty(nameof(PointerSegment<ManagedPointer>.LengthInBytes), BindingFlags.Public | BindingFlags.Instance)?.GetAccessors(false)?.FirstOrDefault();
				if (ptrLen is null || ptrLen.ReturnType != typeof(long))
					throw new InvalidOperationException(StorageException.InvalidPointerName);
				var ptrMove = pg.ReturnType.GetMethod(nameof(PointerSegment<ManagedPointer>.MoveBy), 0, BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(long), typeof(long) }, null);
				if (ptrMove is null)
					throw new InvalidOperationException(StorageException.InvalidPointerName);
				var fillMethod = typeof(ApiSelector).GetMethod(nameof(ApiSelector.FillWithValue), genericCount, BindingFlags.Public | BindingFlags.Static, null, new[] { pg.ReturnType, typeof(T) }, null)?.MakeGenericMethod(pg.ReturnType.GenericTypeArguments[0]);
				if (fillMethod is null)
					throw new InvalidOperationException(StorageException.InvalidPointerName);

				IL.Emit(OpCodes.Ldarg_0);
				IL.Emit(OpCodes.Callvirt, pg);
				IL.Emit(OpCodes.Stloc_1); // var pointerN = storage.PointerN;
				if (requestMethod is not null)
				{
					Label loopStart = IL.DefineLabel(), loopEnd = IL.DefineLabel();
					IL.Emit(OpCodes.Ldarg_0);
					IL.Emit(OpCodes.Ldc_I8, 0L);
					IL.Emit(OpCodes.Stloc_2); // long offset = 0;
					IL.Emit(OpCodes.Ldloc_1);
					IL.Emit(OpCodes.Calli, ptrLen);
					IL.Emit(OpCodes.Stloc_3); // long length = pointerN.LengthInBytes;

					IL.MarkLabel(loopStart);
					IL.Emit(OpCodes.Ldarg_0);
					IL.Emit(OpCodes.Ldloc_0);
					IL.Emit(OpCodes.Ldloc_2);
					IL.Emit(OpCodes.Add);
					IL.Emit(OpCodes.Ldloc_3);
					IL.Emit(OpCodes.Ldc_I4_1);
					IL.Emit(OpCodes.Callvirt, requestMethod);
					IL.Emit(OpCodes.Stloc_S, 4); // long actual = storage.Request(allOffset + offset, length, intentWrite: true);
					IL.Emit(OpCodes.Ldloc_S, 4);
					IL.Emit(OpCodes.Brfalse, loopEnd); // if (actual == 0) goto LOOP_END;

					IL.Emit(OpCodes.Ldarg_0);
					IL.Emit(OpCodes.Ldloc_0);
					IL.Emit(OpCodes.Ldloc_2);
					IL.Emit(OpCodes.Add);
					IL.Emit(OpCodes.Ldloc_S, 4);
					IL.Emit(OpCodes.Ldc_I4_1);
					IL.Emit(OpCodes.Callvirt, requestMethod);
					IL.Emit(OpCodes.Pop); // storage.Request(allOffset + offset, actual, intentWrite: true);
					IL.Emit(OpCodes.Ldloc_1);
					IL.Emit(OpCodes.Ldc_I8, 0L);
					IL.Emit(OpCodes.Ldloc_S, 4);
					IL.Emit(OpCodes.Calli, ptrMove);
					IL.Emit(OpCodes.Ldarg_1);
					IL.Emit(OpCodes.Call, fillMethod); // ApiSelector.FillWithValue(pointerN.MoveBy(0, actual), value);

					IL.Emit(OpCodes.Ldloc_2);
					IL.Emit(OpCodes.Ldloc_S, 4);
					IL.Emit(OpCodes.Add);
					IL.Emit(OpCodes.Stloc_2); // offset += actual;
					IL.Emit(OpCodes.Ldloc_3);
					IL.Emit(OpCodes.Ldloc_S, 4);
					IL.Emit(OpCodes.Sub);
					IL.Emit(OpCodes.Stloc_3); // length -= actual;
					IL.Emit(OpCodes.Ldloc_1);
					IL.Emit(OpCodes.Ldloc_3);
					IL.Emit(OpCodes.Ldc_I8, 0L);
					IL.Emit(OpCodes.Calli, ptrMove);
					IL.Emit(OpCodes.Stloc_1); // pointerN = pointerN.MoveBy(actual);

					IL.Emit(OpCodes.Br_S, loopStart); // goto LOOP_START;

					IL.MarkLabel(loopEnd);
				}
				IL.Emit(OpCodes.Ldloc_1);
				IL.Emit(OpCodes.Ldarg_1);
				IL.Emit(OpCodes.Call, fillMethod); // ApiSelector.FillWithValue(pointerN, value);
				IL.Emit(OpCodes.Ldloc_1);
				IL.Emit(OpCodes.Calli, ptrLen);
				IL.Emit(OpCodes.Ldloc_0);
				IL.Emit(OpCodes.Add);
				if (requestMethod is not null)
				{
					IL.Emit(OpCodes.Ldloc_2);
					IL.Emit(OpCodes.Add);
				}
				IL.Emit(OpCodes.Stloc_0); // allOffset += pointerN.LengthInBytes[ + offset];
			}
			return method.CreateDelegate<Action<TS, T>>();
		}

		private static Func<TS1, TS2, long> GetCopyMethod<TS1, TS2>(int genericCount) where TS1 : class, IStorage where TS2 : class, IStorage
		{
			Type type1 = typeof(TS1), type2 = typeof(TS2);
			var pointerGetters1 = TS1.PointerNames.Select(n => type1.GetProperty(n)?.GetAccessors(false)?.FirstOrDefault());
			var pointerGetters2 = TS2.PointerNames.Select(n => type2.GetProperty(n)?.GetAccessors(false)?.FirstOrDefault());
			var requestMethod1 = type1.GetMethod(nameof(IStorage.Request), 0, BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(long), typeof(long), typeof(bool) }, null);
			var requestMethod2 = type2.GetMethod(nameof(IStorage.Request), 0, BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(long), typeof(long), typeof(bool) }, null);
			DynamicMethod method = new($"Copier from {type1.GetGenericString()}", typeof(long), new[] { type1, type2 });
			var IL = method.GetILGenerator();
			IL.Emit(OpCodes.Ldc_I8, 0L);
			IL.Emit(OpCodes.Stloc_0); // long allOffset = 0;

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
		/// <exception cref="InvalidOperationException">If the <see cref="IStorage.PointerNames"/> of <typeparamref name="TS"/> are not correct pointer property names</exception>
		public static void FillWith<TS>(this TS storage, byte value) where TS : class, IStorage
		{
			if (!storage.IsValid())
				throw new ObjectDisposedException(nameof(storage));

			var handle = typeof(TS).TypeHandle;
			if (fillByteFunc.TryGetValue(handle, out var filler))
				goto FINAL;
			filler = GetFillMethod<TS, byte>(1);
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
		/// <exception cref="InvalidOperationException">If the <see cref="IStorage.PointerNames"/> of <typeparamref name="TS"/> are not correct pointer property names</exception>
		public static void FillWith<T, TS>(this TS storage, T value) where T : unmanaged, INumber<T> where TS : class, IStorage<T, TS>
		{
			if (!storage.IsValid())
				throw new ObjectDisposedException(nameof(storage));

			var handle = typeof(TS).TypeHandle;
			if (fillTFunc.TryGetValue(handle, out var filler))
				goto FINAL;
			filler = GetFillMethod<TS, T>(2);
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
		/// <exception cref="InvalidOperationException">If <paramref name="source"/> overlaps with <paramref name="destination"/> or the <see cref="IStorage.PointerNames"/> of <typeparamref name="TS1"/> or <typeparamref name="TS2"/> are not correct pointer property names</exception>
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
			copier = GetCopyMethod<TS1, TS2>(2);
			copyByteFunc[(handle1, handle2)] = copier;
		FINAL:
			return ((Func<TS1, TS2, long>)copier).Invoke(source, destination);
		}
		#endregion
	}
}