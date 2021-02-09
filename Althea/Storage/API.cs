using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.Linq;
using Althea.NativeTypes;
using Althea.Resources;


namespace Althea.Storage
{
	#region extension
	/// <summary>
	/// The extension methods for <see cref="IPureOrMixedStorage"/> and <see cref="ICachedStorage"/>
	/// </summary>
	internal static class PureMixedAndCachedStorageExtension
	{
		#region casting
		/// <summary>
		/// Get the actual storage of the given <paramref name="storage"/> if it is a <see cref="PureOrMixedStorage{T}"/> or a <see cref="CachedStorage{T}"/>.
		/// </summary>
		/// <typeparam name="T">any unmanaged data type</typeparam>
		/// <param name="storage">The given <see cref="Storage{T}"/> to dereference</param>
		/// <param name="mixed">The output nullable <see cref="IPureOrMixedStorage"/></param>
		/// <param name="cached">The output nullable <see cref="ICachedStorage"/></param>
		/// <returns>The offset in bytes and the length in bytes</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="storage"/> is null or invalid</exception>
		/// <exception cref="NotImplementedException">If <paramref name="storage"/> is neither <see cref="ActualStorage{T}"/> nor <see cref="ReferenceStorage{T}"/> or it is <see cref="ReferenceStorage{T}"/> while its dereference is neither <see cref="PureOrMixedStorage{T}"/> nor <see cref="CachedStorage{T}"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Cast<T>(this Storage<T> storage, out IPureOrMixedStorage? mixed, out ICachedStorage? cached) where T : unmanaged
		{
			if (!storage.IsValid())
				throw new ArgumentNullException(nameof(storage));
			mixed = null; cached = null;
			if (storage is IPureOrMixedStorage mix)
			{
				mixed = mix;
			}
			else if (storage is ICachedStorage cache)
			{
				cached = cache;
			}
			else
				throw new NotImplementedException();
		}

		/// <summary>
		/// Get the length in bytes of the real / underlying actual storage of given <see cref="IPureOrMixedStorage"/>
		/// </summary>
		/// <param name="cached">The given <see cref="IPureOrMixedStorage"/> to obtain length from</param>
		/// <returns>The real length in bytes of <paramref name="cached"/></returns>
		/// <exception cref="InvalidOperationException">If <paramref name="cached"/> is a <see cref="IReferenceStorage"/> while its <see cref="IReferenceStorage.Reference"/> is not a <see cref="IPureOrMixedStorage"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long GetRealLength(this IPureOrMixedStorage cached)
		{
			if (cached is IReferenceStorage r)
			{
				if (r.Reference is IPureOrMixedStorage c)
					return c.LengthInBytes;
				else
					throw new InvalidOperationException();
			}
			else
			{
				return cached.LengthInBytes;
			}
		}

		/// <summary>
		/// Get the length in bytes of the real / underlying actual storage of given <see cref="ICachedStorage"/>
		/// </summary>
		/// <param name="cached">The given <see cref="ICachedStorage"/> to obtain length from</param>
		/// <returns>The real length in bytes of <paramref name="cached"/></returns>
		/// <exception cref="InvalidOperationException">If <paramref name="cached"/> is a <see cref="IReferenceStorage"/> while its <see cref="IReferenceStorage.Reference"/> is not a <see cref="ICachedStorage"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long GetRealLength(this ICachedStorage cached)
		{
			if (cached is IReferenceStorage r)
			{
				if (r.Reference is ICachedStorage c)
					return c.LengthInBytes;
				else
					throw new InvalidOperationException();
			}
			else
			{
				return cached.LengthInBytes;
			}
		}
		#endregion

		#region apply unary function to a ICachedStorage
		/// <summary>
		/// Apply a unary function <paramref name="unaryFunc"/> to the given <see cref="ICachedStorage"/> <paramref name="cached"/>'s <paramref name="count"/> bytes starting from <paramref name="offset"/>
		/// </summary>
		/// <param name="cached">The given <see cref="ICachedStorage"/> to be applied by the <paramref name="unaryFunc"/></param>
		/// <param name="unaryFunc">The unary function whose input argument is the result of <see cref="ICachedStorage.Retrieve(long, long, ICachedStorage.CopyDelegate?)"/></param>
		/// <param name="offset">The staring offset in bytes of <paramref name="cached"/></param>
		/// <param name="count">The number of bytes to of <paramref name="cached"/></param>
		/// <param name="copyFunc">The <see cref="ICachedStorage.CopyDelegate"/> used to invoke <see cref="ICachedStorage.Retrieve(long, long, ICachedStorage.CopyDelegate?)"/></param>
		/// <param name="pack">The internal offsets and lengths during the process shall be divisible by this value</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="pack"/> is 0 or larger than <paramref name="count"/>; or <paramref name="count"/> is out of the boundary of <paramref name="cached"/></exception>
		/// <exception cref="ArgumentException">If <paramref name="count"/> or <paramref name="offset"/> is not divisible by <paramref name="pack"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ApplyUnaryFunction(this ICachedStorage cached, Action<PointerSegment> unaryFunc, long offset, long count, ICachedStorage.CopyDelegate? copyFunc = null, int pack = 1)
		{
			if (pack <= 0)
				throw new ArgumentOutOfRangeException(nameof(pack), Parameter.MustPositive);
			if (pack > count)
				throw new ArgumentOutOfRangeException(nameof(pack), Parameter.InvalidValue);
			if (offset % pack != 0)
				throw new ArgumentException(Other.CannotDivide, nameof(offset));
			if (count % pack != 0)
				throw new ArgumentException(Other.CannotDivide, nameof(count));
			if (count + offset > cached.LengthInBytes)
				throw new ArgumentOutOfRangeException(nameof(count), Parameter.InvalidValue);

			long maxCacheSize = cached.TopCacheSizeInBytes / pack * pack;
			while (count > 0)
			{
				long getLength = Math.Min(count, maxCacheSize);
				var temp = cached.Retrieve(offset, count, copyFunc);
				unaryFunc.Invoke(temp);
				offset += getLength;
				count -= getLength;
			}
		}

		/// <summary>
		/// Apply a unary function <paramref name="unaryFunc"/> to the given <see cref="ICachedStorage"/> <paramref name="cached"/>'s <paramref name="count"/> bytes starting from <paramref name="offset"/>
		/// </summary>
		/// <typeparam name="TRet">The return type of <paramref name="unaryFunc"/>, not used</typeparam>
		/// <param name="cached">The given <see cref="ICachedStorage"/> to be applied by the <paramref name="unaryFunc"/></param>
		/// <param name="unaryFunc">The unary function whose input argument is the result of <see cref="ICachedStorage.Retrieve(long, long, ICachedStorage.CopyDelegate?)"/></param>
		/// <param name="offset">The staring offset in bytes of <paramref name="cached"/></param>
		/// <param name="count">The number of bytes to of <paramref name="cached"/></param>
		/// <param name="copyFunc">The <see cref="ICachedStorage.CopyDelegate"/> used to invoke <see cref="ICachedStorage.Retrieve(long, long, ICachedStorage.CopyDelegate?)"/></param>
		/// <param name="pack">The internal offsets and lengths during the process shall be divisible by this value</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="pack"/> is 0 or larger than <paramref name="count"/>; or <paramref name="count"/> is out of the boundary of <paramref name="cached"/></exception>
		/// <exception cref="ArgumentException">If <paramref name="count"/> or <paramref name="offset"/> is not divisible by <paramref name="pack"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ApplyUnaryFunction<TRet>(this ICachedStorage cached, Func<PointerSegment, TRet> unaryFunc, long offset, long count, ICachedStorage.CopyDelegate? copyFunc = null, int pack = 1)
		{
			if (pack <= 0)
				throw new ArgumentOutOfRangeException(nameof(pack), Parameter.MustPositive);
			if (pack > count)
				throw new ArgumentOutOfRangeException(nameof(pack), Parameter.InvalidValue);
			if (offset % pack != 0)
				throw new ArgumentException(Other.CannotDivide, nameof(offset));
			if (count % pack != 0)
				throw new ArgumentException(Other.CannotDivide, nameof(count));
			if (count + offset > cached.LengthInBytes)
				throw new ArgumentOutOfRangeException(nameof(count), Parameter.InvalidValue);

			long maxCacheSize = cached.TopCacheSizeInBytes / pack * pack;
			while (count > 0)
			{
				long getLength = Math.Min(count, maxCacheSize);
				var temp = cached.Retrieve(offset, count, copyFunc);
				unaryFunc.Invoke(temp);
				offset += getLength;
				count -= getLength;
			}
		}

		/// <summary>
		/// Apply a unary function <paramref name="unaryFunc"/> to the given <see cref="ICachedStorage"/> <paramref name="cached"/>'s <paramref name="count"/> bytes starting from <paramref name="offset"/>
		/// </summary>
		/// <typeparam name="T">any auxiliary data type</typeparam>
		/// <param name="cached">The given <see cref="ICachedStorage"/> to be applied by the <paramref name="unaryFunc"/></param>
		/// <param name="unaryFunc">The unary function whose first input argument is the result of <see cref="ICachedStorage.Retrieve(long, long, ICachedStorage.CopyDelegate?)"/> and the second one is the <paramref name="auxiliary"/></param>
		/// <param name="offset">The staring offset in bytes of <paramref name="cached"/></param>
		/// <param name="count">The number of bytes to of <paramref name="cached"/></param>
		/// <param name="auxiliary">The auxiliary data</param>
		/// <param name="copyFunc">The <see cref="ICachedStorage.CopyDelegate"/> used to invoke <see cref="ICachedStorage.Retrieve(long, long, ICachedStorage.CopyDelegate?)"/></param>
		/// <param name="pack">The internal offsets and lengths during the process shall be divisible by this value</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="pack"/> is 0 or larger than <paramref name="count"/>; or <paramref name="count"/> is out of the boundary of <paramref name="cached"/></exception>
		/// <exception cref="ArgumentException">If <paramref name="count"/> is not divisible by <paramref name="pack"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ApplyUnaryFunction<T>(this ICachedStorage cached, Action<PointerSegment, T> unaryFunc, long offset, long count, T auxiliary, ICachedStorage.CopyDelegate? copyFunc = null, int pack = 1)
		{
			if (pack <= 0)
				throw new ArgumentOutOfRangeException(nameof(pack), Parameter.MustPositive);
			if (pack > count)
				throw new ArgumentOutOfRangeException(nameof(pack), Parameter.InvalidValue);
			if (offset % pack != 0)
				throw new ArgumentException(Other.CannotDivide, nameof(offset));
			if (count % pack != 0)
				throw new ArgumentException(Other.CannotDivide, nameof(count));
			if (count + offset > cached.LengthInBytes)
				throw new ArgumentOutOfRangeException(nameof(count), Parameter.InvalidValue);

			long maxCacheSize = cached.TopCacheSizeInBytes / pack * pack;
			while (count > 0)
			{
				long getLength = Math.Min(count, maxCacheSize);
				var temp = cached.Retrieve(offset, count, copyFunc);
				unaryFunc.Invoke(temp, auxiliary);
				offset += getLength;
				count -= getLength;
			}
		}

		/// <summary>
		/// Apply a unary function <paramref name="unaryFunc"/> to the given <see cref="ICachedStorage"/> <paramref name="cached"/>'s <paramref name="count"/> bytes starting from <paramref name="offset"/>
		/// </summary>
		/// <typeparam name="T">any auxiliary data type</typeparam>
		/// <typeparam name="TRet">The return type of <paramref name="unaryFunc"/>, not used</typeparam>
		/// <param name="cached">The given <see cref="ICachedStorage"/> to be applied by the <paramref name="unaryFunc"/></param>
		/// <param name="unaryFunc">The unary function whose first input argument is the result of <see cref="ICachedStorage.Retrieve(long, long, ICachedStorage.CopyDelegate?)"/> and the second one is the <paramref name="auxiliary"/></param>
		/// <param name="offset">The staring offset in bytes of <paramref name="cached"/></param>
		/// <param name="count">The number of bytes to of <paramref name="cached"/></param>
		/// <param name="auxiliary">The auxiliary data</param>
		/// <param name="copyFunc">The <see cref="ICachedStorage.CopyDelegate"/> used to invoke <see cref="ICachedStorage.Retrieve(long, long, ICachedStorage.CopyDelegate?)"/></param>
		/// <param name="pack">The internal offsets and lengths during the process shall be divisible by this value</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="pack"/> is 0 or larger than <paramref name="count"/>; or <paramref name="count"/> is out of the boundary of <paramref name="cached"/></exception>
		/// <exception cref="ArgumentException">If <paramref name="count"/> is not divisible by <paramref name="pack"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ApplyUnaryFunction<T, TRet>(this ICachedStorage cached, Func<PointerSegment, T, TRet> unaryFunc, long offset, long count, T auxiliary, ICachedStorage.CopyDelegate? copyFunc = null, int pack = 1)
		{
			if (pack <= 0)
				throw new ArgumentOutOfRangeException(nameof(pack), Parameter.MustPositive);
			if (pack > count)
				throw new ArgumentOutOfRangeException(nameof(pack), Parameter.InvalidValue);
			if (offset % pack != 0)
				throw new ArgumentException(Other.CannotDivide, nameof(offset));
			if (count % pack != 0)
				throw new ArgumentException(Other.CannotDivide, nameof(count));
			if (count + offset > cached.LengthInBytes)
				throw new ArgumentOutOfRangeException(nameof(count), Parameter.InvalidValue);

			long maxCacheSize = cached.TopCacheSizeInBytes / pack * pack;
			while (count > 0)
			{
				long getLength = Math.Min(count, maxCacheSize);
				var temp = cached.Retrieve(offset, count, copyFunc);
				unaryFunc.Invoke(temp, auxiliary);
				offset += getLength;
				count -= getLength;
			}
		}

		/// <summary>
		/// Encapsulates a method that delimits and returns the given <paramref name="originalValue"/> with <paramref name="offset"/> and <paramref name="length"/>
		/// </summary>
		/// <typeparam name="T">any data type which can be sliced</typeparam>
		/// <param name="originalValue">The original value in <typeparamref name="T"/></param>
		/// <param name="offset">The offset in <see cref="long"/></param>
		/// <param name="length">The length in <see cref="long"/></param>
		/// <returns>The <paramref name="originalValue"/> after the slice as a <typeparamref name="T"/></returns>
		public delegate T SliceDelegate<T>(T originalValue, long offset, long length);

		/// <summary>
		/// Apply a unary function <paramref name="unaryFunc"/> to the given <see cref="ICachedStorage"/> <paramref name="cached"/>'s <paramref name="count"/> bytes starting from <paramref name="offset"/>
		/// </summary>
		/// <typeparam name="T">any auxiliary data type</typeparam>
		/// <typeparam name="TRet">The return type of <paramref name="unaryFunc"/>, not used</typeparam>
		/// <param name="cached">The given <see cref="ICachedStorage"/> to be applied by the <paramref name="unaryFunc"/></param>
		/// <param name="unaryFunc">The unary function whose first input argument is the result of <see cref="ICachedStorage.Retrieve(long, long, ICachedStorage.CopyDelegate?)"/> and the second one is the output of <paramref name="sliceFunc"/></param>
		/// <param name="offset">The staring offset in bytes of <paramref name="cached"/></param>
		/// <param name="count">The number of bytes to of <paramref name="cached"/></param>
		/// <param name="auxiliaryOriginal">The auxiliary data's initial value, used as the first input argument of <paramref name="sliceFunc"/></param>
		/// <param name="sliceFunc">The <see cref="SliceDelegate{T}"/> used to slice <paramref name="auxiliaryOriginal"/></param>
		/// <param name="copyFunc">The <see cref="ICachedStorage.CopyDelegate"/> used to invoke <see cref="ICachedStorage.Retrieve(long, long, ICachedStorage.CopyDelegate?)"/></param>
		/// <param name="pack">The internal offsets and lengths during the process shall be divisible by this value</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="pack"/> is 0 or larger than <paramref name="count"/>; or <paramref name="count"/> is out of the boundary of <paramref name="cached"/></exception>
		/// <exception cref="ArgumentException">If <paramref name="count"/> is not divisible by <paramref name="pack"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ApplyUnaryFunction<T, TRet>(this ICachedStorage cached, Func<PointerSegment, T, TRet> unaryFunc, long offset, long count, T auxiliaryOriginal, SliceDelegate<T> sliceFunc, ICachedStorage.CopyDelegate? copyFunc = null, int pack = 1)
		{
			if (pack <= 0)
				throw new ArgumentOutOfRangeException(nameof(pack), Parameter.MustPositive);
			if (pack > count)
				throw new ArgumentOutOfRangeException(nameof(pack), Parameter.InvalidValue);
			if (offset % pack != 0)
				throw new ArgumentException(Other.CannotDivide, nameof(offset));
			if (count % pack != 0)
				throw new ArgumentException(Other.CannotDivide, nameof(count));
			if (count + offset > cached.LengthInBytes)
				throw new ArgumentOutOfRangeException(nameof(count), Parameter.InvalidValue);

			long maxCacheSize = cached.TopCacheSizeInBytes / pack * pack;
			while (count > 0)
			{
				long getLength = Math.Min(count, maxCacheSize);
				var temp = cached.Retrieve(offset, count, copyFunc);
				T val = sliceFunc.Invoke(auxiliaryOriginal, offset, getLength);
				unaryFunc.Invoke(temp, val);
				offset += getLength;
				count -= getLength;
			}
		}
		#endregion

		#region copies between IPureOrMixedStorage and ICachedStorage
		/// <summary>
		/// Copy memory from <paramref name="source"/> to <paramref name="destination"/> where both are <see cref="IPureOrMixedStorage"/>.
		/// </summary>
		/// <param name="source">The source <see cref="IPureOrMixedStorage"/> to copy from</param>
		/// <param name="destination">The destination <see cref="IPureOrMixedStorage"/> to copy into</param>
		/// <param name="copyFunc">The <see cref="ICachedStorage.CopyDelegate"/> used to copy data, default is the <see cref="AbstractApi.MemoryCopy(PointerSegment, PointerSegment)"/> selected by <see cref="AbstractApi.SelectImplementation(IStorage, IStorage)"/></param>
		/// <param name="sourceAlign">The number of bytes to align <paramref name="source"/> before invoking <paramref name="copyFunc"/>, default 1 means no alignment.</param>
		/// <param name="destinationAlign">The number of bytes to align <paramref name="destination"/> before invoking <paramref name="copyFunc"/>, default 1 means no alignment.</param>
		/// <remarks>The one with less length among <paramref name="source"/> and <paramref name="destination"/> is used as the actual copy length</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="sourceAlign"/> or <paramref name="destinationAlign"/> is not a positive value or is larger than the lengths of <paramref name="source"/> or <paramref name="destination"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void StorageMemoryCopy(this IPureOrMixedStorage source, IPureOrMixedStorage destination, ICachedStorage.CopyDelegate? copyFunc = null, int sourceAlign = 1, int destinationAlign = 1)
		{
			if (!source.IsValid())
				throw new ArgumentNullException(nameof(source));
			if (!destination.IsValid())
				throw new ArgumentNullException(nameof(destination));
			if (sourceAlign <= 0)
				throw new ArgumentOutOfRangeException(nameof(sourceAlign), Parameter.MustPositive);
			if (sourceAlign >= source.LengthInBytes)
				throw new ArgumentOutOfRangeException(nameof(sourceAlign), Parameter.InvalidValue);
			if (destinationAlign <= 0)
				throw new ArgumentOutOfRangeException(nameof(destinationAlign), Parameter.MustPositive);
			if (destinationAlign >= destination.LengthInBytes)
				throw new ArgumentOutOfRangeException(nameof(destinationAlign), Parameter.InvalidValue);

			copyFunc ??= AbstractApi.SelectImplementation(source, destination).MemoryCopy;

			PointerSegment src = source[0], dst = destination[0];
			bool incSrc = false, incDst = false;
			int i = 0, j = 0;
			long srcOffset = 0, dstOffset = 0;
			while (i < source.Count && j < destination.Count)
			{
				// get next
				if (incSrc)
					src = source[i].MoveBy(srcOffset);
				if (incDst)
					dst = destination[j].MoveBy(dstOffset);
				// copy
				long copyCount = copyFunc.Invoke(src, dst);
				// check next
				long srcNextOffset = copyCount * sourceAlign, dstNextOffset = copyCount * destinationAlign;
				if (srcNextOffset >= src.LengthInBytes)
				{
					incSrc = true; i++;
					srcOffset = srcNextOffset - src.LengthInBytes;
				}
				else
				{
					incSrc = false;
					src += srcNextOffset;
				}
				if (dstNextOffset >= dst.LengthInBytes)
				{
					incDst = true; j++;
					dstOffset = dstNextOffset - dst.LengthInBytes;
				}
				else
				{
					incDst = false;
					dst += dstNextOffset;
				}
			}
		}

		/// <summary>
		/// Copy memory from a <see cref="IPureOrMixedStorage"/> <paramref name="source"/> to a <see cref="ICachedStorage"/> <paramref name="destination"/>.
		/// </summary>
		/// <param name="source">The source <see cref="PureOrMixedStorage{T}"/> to copy from</param>
		/// <param name="destination">The destination <see cref="CachedStorage{T}"/> to copy into</param>
		/// <param name="alignedCopy">The <see cref="ICachedStorage.CopyDelegate"/> used to aligned copy data, default is the same as <paramref name="blockCopy"/></param>
		/// <param name="blockCopy">The <see cref="ICachedStorage.CopyDelegate"/> used to copy data and invoke <see cref="ICachedStorage.Retrieve(long, long, ICachedStorage.CopyDelegate?)"/>, default is the <see cref="AbstractApi.MemoryCopy(PointerSegment, PointerSegment)"/> selected by <see cref="AbstractApi.SelectImplementation(IStorage, IStorage)"/></param>
		/// <param name="sourceAlign">The number of bytes to align <paramref name="source"/> before invoking <paramref name="blockCopy"/>, default 1 means no alignment.</param>
		/// <param name="destinationAlign">The number of bytes to align <paramref name="destination"/> before invoking <paramref name="blockCopy"/>, default 1 means no alignment.</param>
		/// <remarks>The one with less length in <paramref name="source"/> and <paramref name="destination"/> is used as the actual copy length</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="sourceAlign"/> or <paramref name="destinationAlign"/> is not a positive value or is larger than the lengths of <paramref name="source"/> or <paramref name="destination"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void StorageMemoryCopy(this IPureOrMixedStorage source, ICachedStorage destination, ICachedStorage.CopyDelegate? alignedCopy = null, ICachedStorage.CopyDelegate? blockCopy = null, int sourceAlign = 1, int destinationAlign = 1)
		{
			if (!source.IsValid())
				throw new ArgumentNullException(nameof(source));
			if (!destination.IsValid())
				throw new ArgumentNullException(nameof(destination));
			if (sourceAlign <= 0)
				throw new ArgumentOutOfRangeException(nameof(sourceAlign), Parameter.MustPositive);
			if (sourceAlign >= source.LengthInBytes)
				throw new ArgumentOutOfRangeException(nameof(sourceAlign), Parameter.InvalidValue);
			if (destinationAlign <= 0)
				throw new ArgumentOutOfRangeException(nameof(destinationAlign), Parameter.MustPositive);
			if (destinationAlign >= destination.LengthInBytes)
				throw new ArgumentOutOfRangeException(nameof(destinationAlign), Parameter.InvalidValue);

			blockCopy ??= AbstractApi.SelectImplementation(source, destination).MemoryCopy;
			alignedCopy ??= blockCopy;

			long maxCacheSize = destination.TopCacheSizeInBytes;
			long count = Math.Min((source.LengthInBytes - 1) / sourceAlign + 1, (destination.LengthInBytes - 1) / destinationAlign + 1);
			long srcOffset = 0, dstOffset = 0;
			for (int i = 0; i < source.Count; i++)
			{
				var srcOrg = source[i];
				while (count > 0 && srcOffset < srcOrg.LengthInBytes)
				{
					// get
					long dstGet = Math.Min(count * destinationAlign, maxCacheSize);
					var src = srcOrg.MoveBy(srcOffset);
					var dst = destination.Retrieve(dstOffset, dstGet, blockCopy);
					// copy
					long copiedCount = alignedCopy.Invoke(src, dst);
					// increase
					count -= copiedCount;
					srcOffset += copiedCount * sourceAlign;
					dstOffset += copiedCount * destinationAlign;
				}
				srcOffset -= srcOrg.LengthInBytes;
			}
		}

		/// <summary>
		/// Copy memory from a <see cref="ICachedStorage"/> <paramref name="source"/> to a <see cref="IPureOrMixedStorage"/> <paramref name="destination"/>.
		/// </summary>
		/// <param name="source">The source <see cref="PureOrMixedStorage{T}"/> to copy from</param>
		/// <param name="destination">The destination <see cref="CachedStorage{T}"/> to copy into</param>
		/// <param name="alignedCopy">The <see cref="ICachedStorage.CopyDelegate"/> used to aligned copy data, default is the same as <paramref name="blockCopy"/></param>
		/// <param name="blockCopy">The <see cref="ICachedStorage.CopyDelegate"/> used to copy data and invoke <see cref="ICachedStorage.Retrieve(long, long, ICachedStorage.CopyDelegate?)"/>, default is the <see cref="AbstractApi.MemoryCopy(PointerSegment, PointerSegment)"/> selected by <see cref="AbstractApi.SelectImplementation(IStorage, IStorage)"/></param>
		/// <param name="sourceAlign">The number of bytes to align <paramref name="source"/> before invoking <paramref name="blockCopy"/>, default 1 means no alignment.</param>
		/// <param name="destinationAlign">The number of bytes to align <paramref name="destination"/> before invoking <paramref name="blockCopy"/>, default 1 means no alignment.</param>
		/// <remarks>The one with less length among <paramref name="source"/> and <paramref name="destination"/> is used as the actual copy length</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="sourceAlign"/> or <paramref name="destinationAlign"/> is not a positive value or is larger than the lengths of <paramref name="source"/> or <paramref name="destination"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void StorageMemoryCopy(this ICachedStorage source, IPureOrMixedStorage destination, ICachedStorage.CopyDelegate? alignedCopy = null, ICachedStorage.CopyDelegate? blockCopy = null, int sourceAlign = 1, int destinationAlign = 1)
		{
			if (!source.IsValid())
				throw new ArgumentNullException(nameof(source));
			if (!destination.IsValid())
				throw new ArgumentNullException(nameof(destination));
			if (sourceAlign <= 0)
				throw new ArgumentOutOfRangeException(nameof(sourceAlign), Parameter.MustPositive);
			if (sourceAlign >= source.LengthInBytes)
				throw new ArgumentOutOfRangeException(nameof(sourceAlign), Parameter.InvalidValue);
			if (destinationAlign <= 0)
				throw new ArgumentOutOfRangeException(nameof(destinationAlign), Parameter.MustPositive);
			if (destinationAlign >= destination.LengthInBytes)
				throw new ArgumentOutOfRangeException(nameof(destinationAlign), Parameter.InvalidValue);

			blockCopy ??= AbstractApi.SelectImplementation(source, destination).MemoryCopy;
			alignedCopy ??= blockCopy;

			long maxCacheSize = source.TopCacheSizeInBytes;
			long count = Math.Min((source.LengthInBytes - 1) / sourceAlign + 1, (destination.LengthInBytes - 1) / destinationAlign + 1);
			long srcOffset = 0, dstOffset = 0;
			for (int i = 0; i < destination.Count; i++)
			{
				var dstOrg = destination[i];
				while (count > 0 && dstOffset < dstOrg.LengthInBytes)
				{
					// get
					long srcGet = Math.Min(count * sourceAlign, maxCacheSize);
					var dst = dstOrg.MoveBy(dstOffset);
					var src = source.Retrieve(srcOffset, srcGet, blockCopy);
					// copy
					long copiedCount = alignedCopy.Invoke(src, dst);
					// increase
					count -= copiedCount;
					srcOffset += copiedCount * sourceAlign;
					dstOffset += copiedCount * destinationAlign;
				}
				dstOffset -= dstOrg.LengthInBytes;
			}
		}

		/// <summary>
		/// Copy memory from <paramref name="source"/> to <paramref name="destination"/> where both are <see cref="ICachedStorage"/>.
		/// </summary>
		/// <param name="source">The source <see cref="ICachedStorage"/> to copy from</param>
		/// <param name="destination">The destination <see cref="ICachedStorage"/> to copy into</param>
		/// <param name="alignedCopy">The <see cref="ICachedStorage.CopyDelegate"/> used to aligned copy data, default is the same as <paramref name="blockCopy"/></param>
		/// <param name="blockCopy">The <see cref="ICachedStorage.CopyDelegate"/> used to copy data and invoke <see cref="ICachedStorage.Retrieve(long, long, ICachedStorage.CopyDelegate?)"/>, default is the <see cref="AbstractApi.MemoryCopy(PointerSegment, PointerSegment)"/> selected by <see cref="AbstractApi.SelectImplementation(IStorage, IStorage)"/></param>
		/// <param name="sourceAlign">The number of bytes to align <paramref name="source"/> before invoking <paramref name="blockCopy"/>, default 1 means no alignment.</param>
		/// <param name="destinationAlign">The number of bytes to align <paramref name="destination"/> before invoking <paramref name="blockCopy"/>, default 1 means no alignment.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is null or empty</exception>
		/// <remarks>The one with less length among <paramref name="source"/> and <paramref name="destination"/> is used as the actual copy length</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="sourceAlign"/> or <paramref name="destinationAlign"/> is not a positive value or is larger than the lengths of <paramref name="source"/> or <paramref name="destination"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void StorageMemoryCopy(this ICachedStorage source, ICachedStorage destination, ICachedStorage.CopyDelegate? alignedCopy = null, ICachedStorage.CopyDelegate? blockCopy = null, int sourceAlign = 1, int destinationAlign = 1)
		{
			if (!source.IsValid())
				throw new ArgumentNullException(nameof(source));
			if (!destination.IsValid())
				throw new ArgumentNullException(nameof(destination));
			if (sourceAlign <= 0)
				throw new ArgumentOutOfRangeException(nameof(sourceAlign), Parameter.MustPositive);
			if (sourceAlign >= source.LengthInBytes)
				throw new ArgumentOutOfRangeException(nameof(sourceAlign), Parameter.InvalidValue);
			if (destinationAlign <= 0)
				throw new ArgumentOutOfRangeException(nameof(destinationAlign), Parameter.MustPositive);
			if (destinationAlign >= destination.LengthInBytes)
				throw new ArgumentOutOfRangeException(nameof(destinationAlign), Parameter.InvalidValue);

			blockCopy ??= AbstractApi.SelectImplementation(source, destination).MemoryCopy;
			alignedCopy ??= blockCopy;

			long maxCacheSize = Math.Min(source.TopCacheSizeInBytes, destination.TopCacheSizeInBytes);
			long count = Math.Min((source.LengthInBytes - 1) / sourceAlign + 1, (destination.LengthInBytes - 1) / destinationAlign + 1);
			long srcOffset = 0, dstOffset = 0;
			while (count > 0)
			{
				// get
				long srcGet = Math.Min(count * sourceAlign, maxCacheSize), dstGet = Math.Min(count * destinationAlign, maxCacheSize);
				var src = source.Retrieve(srcOffset, srcGet, blockCopy);
				var dst = destination.Retrieve(dstOffset, dstGet, blockCopy);
				// copy
				long copiedCount = alignedCopy.Invoke(src, dst);
				// increase
				count -= copiedCount;
				srcOffset += copiedCount * sourceAlign;
				dstOffset += copiedCount * destinationAlign;
			}
		}
		#endregion
	}
	#endregion


	/// <summary>
	/// The abstract class for runtime memory API routines 
	/// </summary>
	/// <remarks>The default implementations of methods about <see cref="Storage{T}"/> ensure that you can only implement the basic low-level memory operations of "pure" storages while the high-level methods about <see cref="Storage{T}"/> work fine automatically. However, if there are native supports, it is still recommended to overwrite these methods.</remarks>
	public abstract class AbstractApi : AbstractRuntimeApi
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
		/// Special version for <see cref="AbstractApi"/> of method <see cref="AbstractRuntimeApi.SelectImplementation{T}(LinkedList{T}, StorageLocation)"/>
		/// </summary>
		public static AbstractApi SelectImplementation(StorageLocation location) => SelectImplementation(RecentAPIs, location);

		/// <summary>
		/// Special version for <see cref="AbstractApi"/> of method <see cref="AbstractRuntimeApi.SelectImplementation{T}(LinkedList{T}, IStorage)"/>
		/// </summary>
		public static AbstractApi SelectImplementation(IStorage storage) => SelectImplementation(RecentAPIs, storage);

		/// <summary>
		/// Special version for <see cref="AbstractApi"/> of method <see cref="AbstractRuntimeApi.SelectImplementation{T}(LinkedList{T}, IStorage, IStorage)"/>
		/// </summary>
		public static AbstractApi SelectImplementation(IStorage storage1, IStorage storage2) => SelectImplementation(RecentAPIs, storage1, storage2);
		#endregion


		#region support information
		/// <summary>
		/// Get list of the supported <see cref="CombinationOfLocations"/> for all ternary operations. Since <see cref="AbstractApi"/> has no definition of ternary operations, this override returns null.
		/// </summary>
		public override IReadOnlyList<ImmutableThreeElementSet<CombinationOfLocations>> SupportedTernaryLocations => Array.Empty<ImmutableThreeElementSet<CombinationOfLocations>>();

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

		#region properties
		/// <summary>
		/// When implemented by a derived class, get the underlying driver's version of a supported <see cref="LocationType"/>.
		/// </summary>
		/// <param name="location">The given supported <see cref="LocationType"/></param>
		/// <returns>The underlying driver's version of given <paramref name="location"/></returns>
		public abstract (int major, int minor) DriverVersion(LocationType location);

		/// <summary>
		/// When implemented by a derived class, get the maximum number of devices available of a supported <see cref="LocationType"/>.
		/// </summary>
		/// <param name="location">The given supported <see cref="LocationType"/></param>
		/// <returns>The maximum number of devices available of given <paramref name="location"/></returns>
		public abstract int MaxDeviceNumber(LocationType location);

		/// <summary>
		/// When implemented by a derived class, get the available and total memory in bytes for device indicated by a supported <see cref="StorageLocation"/>.
		/// </summary>
		/// <param name="location">The given supported <see cref="StorageLocation"/></param>
		/// <returns>The available and total memory in bytes of device of given <paramref name="location"/></returns>
		public abstract (long free, long total) FreeAndTotalMemory(StorageLocation location);
		#endregion

		#region low-level storage operations
		/// <summary>
		/// When implemented by a derived class, allocate a storage at given <paramref name="location"/> with given <paramref name="length"/>
		/// </summary>
		/// <param name="location">The <see cref="StorageLocation"/> to allocate on</param>
		/// <param name="length">The length to allocate in bytes</param>
		/// <returns>The allocated pointer as a <see cref="PointerSegment"/></returns>
		/// <exception cref="NotSupportedException">If <paramref name="location"/> is not supported</exception>
		/// <exception cref="InvalidOperationException">If this implementation cannot allocate <paramref name="length"/> on <paramref name="location"/> due to other issues such as <see cref="OutOfMemoryException"/></exception>
		/// <remarks>This methods shall <b>never</b> be exposed publicly to prevent unexpected memory leaks which GC cannot collect due to improper usage.</remarks>
		protected internal abstract PointerSegment Allocate(StorageLocation location, long length);

		/// <summary>
		/// When implemented by a derived class, allocate a storage at given <paramref name="location"/> with given <paramref name="length"/>.<br/>The default implementation utilizes <see cref="Allocate(StorageLocation, long)"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="location">The <see cref="StorageLocation"/> to allocate on</param>
		/// <param name="length">The length to allocate in <typeparamref name="T"/> rather than bytes</param>
		/// <returns>The allocated pointer as a <see cref="PointerSegment"/></returns>
		/// <exception cref="NotSupportedException">If <paramref name="location"/> is not supported</exception>
		/// <exception cref="InvalidOperationException">If this implementation cannot allocate <paramref name="length"/> on <paramref name="location"/> due to other issues such as <see cref="OutOfMemoryException"/></exception>
		/// <remarks>This methods shall <b>never</b> be exposed publicly to prevent unexpected memory leaks which GC cannot collect due to improper usage.</remarks>
		protected internal virtual PointerSegment Allocate<T>(StorageLocation location, long length) where T : unmanaged => this.Allocate(location, length * Storage<T>.SizeOfT);

		/// <summary>
		/// When implemented by a derived class, free a storage indicated by a given <paramref name="pointer"/>
		/// </summary>
		/// <param name="pointer">The <see cref="PointerSegment"/> to free</param>
		/// <param name="disposeManaged">dispose managed resources held by <paramref name="pointer"/>'s <see cref="PointerSegment.Pointer"/> or not</param>
		/// <returns>If <paramref name="pointer"/> is not supported or <paramref name="pointer"/> is not valid, return false; otherwise, return true.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pointer"/> is invalid</exception>
		/// <remarks>This methods shall <b>never</b> be exposed publicly to prevent unexpected wild pointers due to improper usage.</remarks>
		protected internal abstract bool Free(PointerSegment pointer, bool disposeManaged = true);

		/// <summary>
		/// When implemented by a derived class, fill the <paramref name="pointer"/> by same <paramref name="value"/>, byte by byte.
		/// </summary>
		/// <param name="pointer">The pointer to be filled</param>
		/// <param name="value">The value to set as a <see cref="byte"/></param>
		/// <exception cref="NotSupportedException">If <paramref name="pointer"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="pointer"/> is invalid</exception>
		public abstract void FillWithValue(PointerSegment pointer, byte value);

		/// <summary>
		/// When implemented by a derived class, fill the <paramref name="pointer"/>'s each value by same <paramref name="value"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="pointer">The pointer to be filled</param>
		/// <param name="value">The value to set as a <typeparamref name="T"/></param>
		/// <exception cref="NotSupportedException">if <paramref name="pointer"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="pointer"/> is invalid</exception>
		public abstract void FillWithValue<T>(PointerSegment pointer, T value) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, copy memory from <paramref name="source"/> to <paramref name="destination"/>.
		/// </summary>
		/// <param name="source">The source pointer to copy from</param>
		/// <param name="destination">The destination pointer to copy into</param>
		/// <returns>The number of bytes of actually copied block</returns>
		/// <remarks>The one with less length in <paramref name="source"/> and <paramref name="destination"/> is used as the actual copy length in bytes</remarks>
		/// <exception cref="NotSupportedException">If copy between <paramref name="source"/> and <paramref name="destination"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is invalid</exception>
		public abstract long MemoryCopy(PointerSegment source, PointerSegment destination);

		/// <summary>
		/// When implemented by a derived class, copy 2D data from <paramref name="source"/> to <paramref name="destination"/>.
		/// </summary>
		/// <param name="source">The source pointer</param>
		/// <param name="sourceLD">The source array actual height (actual leading dimension) in bytes</param>
		/// <param name="destination">The destination pointer</param>
		/// <param name="destinationLD">The destination array actual height (actual leading dimension) in bytes</param>
		/// <param name="height">The height to copy in bytes</param>
		/// <param name="width">The width to copy in the real type</param>
		/// <remarks>The lengths of <paramref name="source"/> and <paramref name="destination"/> are ignored</remarks>
		/// <exception cref="NotSupportedException">If copy between <paramref name="source"/> and <paramref name="destination"/> is not supported</exception>
		/// <exception cref="ArgumentOutOfRangeException">If any of the parameters is zero</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is invalid</exception>
		/// <exception cref="ArgumentException">
		/// If <paramref name="height"/> is larger than <paramref name="sourceLD"/> or <paramref name="destinationLD"/>,
		/// or <paramref name="height"/> is larger than <paramref name="sourceLD"/> or <paramref name="destinationLD"/>,
		/// or <c><paramref name="sourceLD"/> * <paramref name="width"/> &gt; <paramref name="source"/>.<see cref="IStorage.LengthInBytes">Length</see></c>, 
		/// or <c><paramref name="destinationLD"/> * <paramref name="width"/> &gt; <paramref name="destination"/>.<see cref="IStorage.LengthInBytes">Length</see></c>
		/// </exception>
		public abstract void MemoryCopy2D(PointerSegment source, long sourceLD, PointerSegment destination, long destinationLD, long height, long width);

		/// <summary>
		/// TWhen implemented by a derived class, copy the <paramref name="source"/> storage to <paramref name="destination"/> storage with given strides.<br/>
		/// <c><paramref name="destination"/>[j] = <paramref name="source"/>[k] for i = 1,¡­,n; k = 1 + (i - 1)*<paramref name="incrementSource"/>, j = 1 + (i - 1)*<paramref name="incrementDestination"/></c>.<br/>
		/// The number of elements copied is calculated to the maximum possible value that does not exceeds the boundaries.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="source">The <see cref="PointerSegment"/> to copy from</param>
		/// <param name="incrementSource">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="source"/></param>
		/// <param name="destination">The <see cref="PointerSegment"/> to copy to</param>
		/// <param name="incrementDestination">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="destination"/></param>
		/// <returns>The number of elements (in <typeparamref name="T"/>) of actually copied block</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/>is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="incrementSource"/> or <paramref name="incrementDestination"/> is less than 1</exception>
		public abstract long StridedCopy<T>(PointerSegment source, int incrementSource, PointerSegment destination, int incrementDestination) where T : unmanaged;
		#endregion

		#region low-level storage and managed operations
		/// <summary>
		/// When implemented by a derived class, copy out the <b>first</b> element in unmanaged pointer <paramref name="source"/> to a managed value of type <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="source">The source <see cref="PointerSegment"/> to copy from</param>
		/// <returns>The first element in <paramref name="source"/>.</returns>
		/// <exception cref="NotSupportedException">if <paramref name="source"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is invalid</exception>
		public abstract T ToManaged<T>(PointerSegment source) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, overwrite the <b>first</b> element in unmanaged pointer <paramref name="destination"/> by a managed <paramref name="value"/> of type <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="destination">The destination <see cref="PointerSegment"/> to copy to</param>
		/// <param name="value">The value of type <typeparamref name="T"/> to copy from</param>
		/// <returns>The number of elements (in <typeparamref name="T"/>) of actually copied block</returns>
		/// <exception cref="NotSupportedException">if <paramref name="destination"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="destination"/> is invalid</exception>
		public abstract long FromManaged<T>(PointerSegment destination, T value) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, copy out the first few elements in unmanaged pointer <paramref name="source"/> to a managed array of type <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="source">The source <see cref="PointerSegment"/> to copy from</param>
		/// <param name="destination">The managed <see cref="ArraySegment{T}"/> of type <typeparamref name="T"/> to copy to</param>
		/// <returns>The number of elements (in <typeparamref name="T"/>) of actually copied block</returns>
		/// <exception cref="NotSupportedException">if <paramref name="source"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is invalid</exception>
		public abstract long ToManaged<T>(PointerSegment source, ArraySegment<T> destination) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, overwrite the first few elements in unmanaged pointer <paramref name="destination"/> by the <paramref name="values"/> of a managed array of type <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="destination">The destination <see cref="PointerSegment"/> to copy to</param>
		/// <param name="values">The managed <see cref="ArraySegment{T}"/> of type <typeparamref name="T"/> to copy from</param>
		/// <returns>The number of elements (in <typeparamref name="T"/>) of actually copied block</returns>
		/// <exception cref="NotSupportedException">if <paramref name="destination"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="destination"/> is invalid</exception>
		public abstract long FromManaged<T>(PointerSegment destination, ArraySegment<T> values) where T : unmanaged;

		// Ignore Spelling: sizeof
		/// <summary>
		/// When implemented by a derived class, copy out the elements in unmanaged pointer <paramref name="source"/> as a 2D matrix to a managed array of type <typeparamref name="T"/> (viewed as a 1D array).
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="source">The source <see cref="PointerSegment"/> to copy from</param>
		/// <param name="leadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="source"/></param>
		/// <param name="height">The height to copy in <typeparamref name="T"/></param>
		/// <param name="width">The width to copy in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="destination">The managed <see cref="ArraySegment{T}"/> of type <typeparamref name="T"/> to copy to, must has <c><see cref="Array.Length">Length</see> ¡Ý <paramref name="height"/> <paramref name="width"/></c></param>
		/// <param name="destinationLeadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="destination"/>, default 0 means <paramref name="height"/></param>
		/// <exception cref="NotSupportedException">if <paramref name="source"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is invalid</exception>
		/// <exception cref="ArgumentException">
		/// If <paramref name="height"/> is larger than <paramref name="leadDim"/> or <paramref name="destinationLeadDim"/>,
		/// or <c><paramref name="leadDim"/> * <paramref name="width"/> * sizeof(<typeparamref name="T"/>) &gt; <paramref name="source"/>.<see cref="PointerSegment.LengthInBytes">Length</see></c>,
		/// or <c><paramref name="destinationLeadDim"/> * <paramref name="width"/> &gt; <paramref name="destination"/></c>.<see cref="Array.Length">Length</see>
		/// </exception>
		public abstract void ToManaged2D<T>(PointerSegment source, long leadDim, long height, long width, ArraySegment<T> destination, long destinationLeadDim = 0) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, overwrite some of the elements in unmanaged pointer <paramref name="destination"/> as a 2D matrix by  a managed array of type <typeparamref name="T"/> (viewed as a 1D array).
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="destination">The destination <see cref="PointerSegment"/> to copy to</param>
		/// <param name="leadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="destination"/></param>
		/// <param name="height">The height to copy in <typeparamref name="T"/></param>
		/// <param name="width">The width to copy in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="values">The managed <see cref="ArraySegment{T}"/> of type <typeparamref name="T"/> to copy from, must has <c><see cref="Array.Length">Length</see> ¡Ý <paramref name="height"/> * <paramref name="width"/></c></param>
		/// <param name="valuesLeadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="values"/>, default 0 means <paramref name="height"/></param>
		/// <exception cref="NotSupportedException">if <paramref name="destination"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="destination"/> is invalid</exception>
		/// <exception cref="ArgumentException">
		/// If <paramref name="height"/> is larger than <paramref name="leadDim"/> or <paramref name="valuesLeadDim"/>,
		/// or <c><paramref name="leadDim"/> * <paramref name="width"/> * sizeof(<typeparamref name="T"/>) &gt; <paramref name="destination"/>.<see cref="PointerSegment.LengthInBytes">Length</see></c>,
		/// or <c><paramref name="valuesLeadDim"/> * <paramref name="width"/> &gt; <paramref name="values"/></c>.<see cref="Array.Length">Length</see>
		/// </exception>
		public abstract void FromManaged2D<T>(PointerSegment destination, long leadDim, long height, long width, ArraySegment<T> values, long valuesLeadDim = 0) where T : unmanaged;
		#endregion

		#region high-level storage operations
		/// <summary>
		/// When implemented by a derived class, fill the <paramref name="storage"/> byte by byte to the same <paramref name="value"/>.<br/>The default implementation only works for <see cref="Storage{T}.LocationDescription"/>.<see cref="CombinationOfLocations.Type">Type</see> == <see cref="CombinationType.PureOrMixed"/> or <see cref="CombinationType.Cached"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="storage">The <see cref="Storage{T}"/> to be filled</param>
		/// <param name="value">The value to fill</param>
		/// <exception cref="NotSupportedException">If <paramref name="storage"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="storage"/> is null or invalid</exception>
		public virtual void FillWithValue<T>(Storage<T> storage, byte value) where T : unmanaged
		{
			storage.Cast(out IPureOrMixedStorage? mixed, out ICachedStorage? cached);
			if (mixed is not null)
			{
				for (int i = 0; i < storage.Count; i++)
				{
					this.FillWithValue(storage[i], value);
				}
			}
			else if (cached is not null)
			{
				if (cached.LengthInBytes <= cached.GetRealLength() * ICachedStorage.CacheSizeRatio)
				{
					cached.Flush();
					this.FillWithValue(cached[0], value);
				}
				else
				{
					cached.ApplyUnaryFunction(this.FillWithValue, 0, cached.LengthInBytes, auxiliary: value, copyFunc: this.MemoryCopy);
				}
			}
		}

		/// <summary>
		/// When implemented by a derived class, fill the <paramref name="storage"/>'s each value by same <paramref name="value"/>.<br/>The default implementation only works for <see cref="Storage{T}.LocationDescription"/>.<see cref="CombinationOfLocations.Type">Type</see> == <see cref="CombinationType.PureOrMixed"/> or <see cref="CombinationType.Cached"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="storage">The <see cref="Storage{T}"/> to be filled</param>
		/// <param name="value">The value to fill</param>
		/// <exception cref="NotSupportedException">If <paramref name="storage"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="storage"/> is null or invalid</exception>
		public virtual void FillWithValue<T>(Storage<T> storage, T value) where T : unmanaged, IEquatable<T>
		{
			storage.Cast(out IPureOrMixedStorage? mixed, out ICachedStorage? cached);
			if (mixed is not null)
			{
				if (value.IsZero())
					for (int i = 0; i < storage.Count; i++)
						this.FillWithValue(storage[i], value);
				else
					for (int i = 0; i < storage.Count; i++)
						this.FillWithValue(storage[i], (byte)0);
			}
			else if (cached is not null)
			{
				if (cached.LengthInBytes <= cached.GetRealLength() * ICachedStorage.CacheSizeRatio)
				{
					cached.Flush();
					if (value.IsZero())
						this.FillWithValue(cached[0], (byte)0);
					else
						this.FillWithValue(cached[0], value);
				}
				else
				{
					if (value.IsZero())
						cached.ApplyUnaryFunction(this.FillWithValue, 0, cached.LengthInBytes, auxiliary: (byte)0, copyFunc: this.MemoryCopy);
					else
						cached.ApplyUnaryFunction(this.FillWithValue, 0, cached.LengthInBytes, auxiliary: value, copyFunc: this.MemoryCopy);
				}
			}
		}

		/// <summary>
		/// When implemented by a derived class, copy memory from <paramref name="source"/> to <paramref name="destination"/>.<br/>The default implementation only works for <see cref="Storage{T}.LocationDescription"/>.<see cref="CombinationOfLocations.Type">Type</see> == <see cref="CombinationType.PureOrMixed"/> or <see cref="CombinationType.Cached"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="source">The source <see cref="Storage{T}"/> to copy from</param>
		/// <param name="destination">The destination <see cref="Storage{T}"/> pointer to copy into</param>
		/// <returns>The number of elements (in <typeparamref name="T"/>) of actually copied block</returns>
		/// <remarks>The one with less length among <paramref name="source"/> and <paramref name="destination"/> is used as the actual copy length</remarks>
		/// <exception cref="NotSupportedException">If <paramref name="source"/> or <paramref name="destination"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is null or invalid</exception>
		public virtual long MemoryCopy<T>(Storage<T> source, Storage<T> destination) where T : unmanaged
		{
			source.Cast(out IPureOrMixedStorage? srcMixed, out ICachedStorage? srcCached);
			destination.Cast(out IPureOrMixedStorage? dstMixed, out ICachedStorage? dstCached);
			// normal cases
			if (srcMixed is not null && dstMixed is not null)
			{
				// shortcut
				if (source.Count == 1 && destination.Count == 1)
				{
					this.MemoryCopy(source[0], destination[0]);
				}
				else
				{
					srcMixed.StorageMemoryCopy(dstMixed, this.MemoryCopy);
				}
			}
			else if (srcMixed is not null && dstCached is not null)
			{
				srcMixed.StorageMemoryCopy(dstCached, this.MemoryCopy);
			}
			else if (srcCached is not null && dstMixed is not null)
			{
				srcCached.StorageMemoryCopy(dstMixed, this.MemoryCopy);
			}
			else if (srcCached is not null && dstCached is not null)
			{
				srcCached.StorageMemoryCopy(dstCached, this.MemoryCopy);
			}
			return Math.Min(source.Length, destination.Length);
		}

		/// <summary>
		/// When implemented by a derived class, copy the <paramref name="source"/> storage to <paramref name="destination"/> storage with given strides.<br/>
		/// <c><paramref name="destination"/>[j] = <paramref name="source"/>[k] for i = 1,¡­,n; k = 1 + (i - 1)*<paramref name="incrementSource"/>, j = 1 + (i - 1)*<paramref name="incrementDestination"/></c>.<br/>
		/// The number of elements copied is calculated to the maximum possible value that does not exceeds the boundaries.<br/>
		/// The default implementation only works for <see cref="Storage{T}.LocationDescription"/>.<see cref="CombinationOfLocations.Type">Type</see> == <see cref="CombinationType.PureOrMixed"/> or <see cref="CombinationType.Cached"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="source">The <see cref="Storage{T}"/> to copy from</param>
		/// <param name="incrementSource">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="source"/></param>
		/// <param name="destination">The <see cref="Storage{T}"/> to copy to</param>
		/// <param name="incrementDestination">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="destination"/></param>
		/// <returns>The number of elements (in <typeparamref name="T"/>) of actually copied block</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="incrementSource"/> or <paramref name="incrementDestination"/> is less than 1</exception>
		public virtual long StridedCopy<T>(Storage<T> source, int incrementSource, Storage<T> destination, int incrementDestination) where T : unmanaged
		{
			long srcLen = source.Length, dstLen = destination.Length;
			if (incrementSource <= 0 || incrementSource >= srcLen)
				throw new ArgumentOutOfRangeException(nameof(incrementSource));
			if (incrementDestination <= 0 || incrementDestination >= dstLen)
				throw new ArgumentOutOfRangeException(nameof(incrementDestination));

			int srcAlign = Storage<T>.SizeOfT * incrementSource, dstAlign = Storage<T>.SizeOfT * incrementDestination;
			long copyFunc(PointerSegment s, PointerSegment d) => this.StridedCopy<T>(s, incrementSource, d, incrementDestination);
			source.Cast(out IPureOrMixedStorage? srcMixed, out ICachedStorage? srcCached);
			destination.Cast(out IPureOrMixedStorage? dstMixed, out ICachedStorage? dstCached);
			// normal cases
			if (srcMixed is not null && dstMixed is not null)
			{
				// shortcut
				if (source.Count == 1 && destination.Count == 1)
				{
					this.StridedCopy<T>(source[0], incrementSource, destination[0], incrementDestination);
				}
				else
				{
					srcMixed.StorageMemoryCopy(dstMixed, copyFunc, sourceAlign: srcAlign, destinationAlign: dstAlign);
				}
			}
			else if (srcMixed is not null && dstCached is not null)
			{
				srcMixed.StorageMemoryCopy(dstCached, alignedCopy: copyFunc, blockCopy: this.MemoryCopy, sourceAlign: srcAlign, destinationAlign: dstAlign);
			}
			else if (srcCached is not null && dstMixed is not null)
			{
				srcCached.StorageMemoryCopy(dstMixed, alignedCopy: copyFunc, blockCopy: this.MemoryCopy, sourceAlign: srcAlign, destinationAlign: dstAlign);
			}
			else if (srcCached is not null && dstCached is not null)
			{
				srcCached.StorageMemoryCopy(dstCached, alignedCopy: copyFunc, blockCopy: this.MemoryCopy, sourceAlign: srcAlign, destinationAlign: dstAlign);
			}
			return Math.Min((srcLen - 1) / incrementSource + 1, (dstLen - 1) / incrementDestination + 1);
		}

		/// <summary>
		/// When implemented by a derived class, copy 2D data from <paramref name="source"/> to <paramref name="destination"/>.<br/>The default implementation only works for <see cref="Storage{T}.LocationDescription"/>.<see cref="CombinationOfLocations.Type">Type</see> == <see cref="CombinationType.PureOrMixed"/> or <see cref="CombinationType.Cached"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="source">The source <see cref="Storage{T}"/></param>
		/// <param name="sourceLD">The source array actual height (actual leading dimension) in <typeparamref name="T"/></param>
		/// <param name="destination">The destination <see cref="Storage{T}"/></param>
		/// <param name="destLD">The destination array actual height (actual leading dimension) in <typeparamref name="T"/></param>
		/// <param name="height">The height to copy in <typeparamref name="T"/></param>
		/// <param name="width">The width to copy in the real type</param>
		/// <remarks>The lengths of <paramref name="source"/> and <paramref name="destination"/> are <b>not</b> ignored</remarks>
		/// <exception cref="NotSupportedException">If <paramref name="source"/> or <paramref name="destination"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If any of the parameters is zero</exception>
		/// <exception cref="ArgumentException">
		/// If <paramref name="height"/> is larger than <paramref name="sourceLD"/> or <paramref name="destLD"/>,
		/// or <c><paramref name="sourceLD"/> * <paramref name="width"/> &gt; <paramref name="source"/>.<see cref="Storage{T}.Length">Length</see></c>, 
		/// or <c><paramref name="destLD"/> * <paramref name="width"/> &gt; <paramref name="destination"/>.<see cref="Storage{T}.Length">Length</see></c>
		/// </exception>
		public virtual void MemoryCopy2D<T>(Storage<T> source, long sourceLD, Storage<T> destination, long destLD, long height, long width) where T : unmanaged
		{
			if (sourceLD == 0)
				throw new ArgumentOutOfRangeException(nameof(sourceLD), Resources.Parameter.MustPositive);
			if (destLD == 0)
				throw new ArgumentOutOfRangeException(nameof(destLD), Resources.Parameter.MustPositive);
			if (width == 0)
				throw new ArgumentOutOfRangeException(nameof(width), Resources.Parameter.MustPositive);
			if (height == 0)
				throw new ArgumentOutOfRangeException(nameof(height), Resources.Parameter.MustPositive);
			if (height > sourceLD || height > destLD)
				throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(height));
			if (sourceLD * width > source.LengthInBytes)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(source));
			if (destLD * width > destination.LengthInBytes)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(destination));

			source.Cast(out IPureOrMixedStorage? srcMixed, out ICachedStorage? _);
			destination.Cast(out IPureOrMixedStorage? dstMixed, out ICachedStorage? _);
			// shortcut
			if (srcMixed is not null && dstMixed is not null && source.Count == 1 && destination.Count == 1)
			{
				this.MemoryCopy2D(source[0], sourceLD, destination[0], destLD, height, width);
				return;
			}
			long srcLD = sourceLD, dstLD = destLD;
			source = source.MakeReference(newLength: height);
			destination = destination.MakeReference(newLength: height);
			for (long column = 0; column < width - 1; column++)
			{
				this.MemoryCopy(source, destination);
				source = source.MakeReference(offset: srcLD, newLength: height);
				destination = destination.MakeReference(offset: dstLD, newLength: height);
			}
			// copy last column
			this.MemoryCopy(source, destination);
		}
		#endregion

		#region high-level storage and managed operations
		/// <summary>
		/// When implemented by a derived class, copy out the <b>first</b> element in <see cref="Storage{T}"/> <paramref name="source"/> to a managed value of type <typeparamref name="T"/>.<br/>The default implementation only works for <see cref="Storage{T}.LocationDescription"/>.<see cref="CombinationOfLocations.Type">Type</see> == <see cref="CombinationType.PureOrMixed"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="source">The source <see cref="Storage{T}"/> to copy from</param>
		/// <returns>The first element in <paramref name="source"/>.</returns>
		/// <exception cref="NotSupportedException">if <paramref name="source"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		public virtual T ToManaged<T>(Storage<T> source) where T : unmanaged
		{
			source.Cast(out IPureOrMixedStorage? mixed, out ICachedStorage? cached);
			if (mixed is not null)
			{
				return this.ToManaged<T>(mixed[0]);
			}
			else if (cached is not null)
			{
				var temp = cached.Retrieve(0, Storage<T>.SizeOfT);
				return this.ToManaged<T>(temp);
			}
			else // never here
				return default;
		}

		/// <summary>
		/// When implemented by a derived class, overwrite the <b>first</b> element in unmanaged pointer <paramref name="destination"/> by a managed <paramref name="value"/> of type <typeparamref name="T"/>.<br/>The default implementation only works for <see cref="Storage{T}.LocationDescription"/>.<see cref="CombinationOfLocations.Type">Type</see> == <see cref="CombinationType.PureOrMixed"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="destination">The destination <see cref="Storage{T}"/> to copy to</param>
		/// <param name="value">The value of type <typeparamref name="T"/> to copy from</param>
		/// <exception cref="NotSupportedException">if <paramref name="destination"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="destination"/> is null or invalid</exception>
		public virtual void FromManaged<T>(Storage<T> destination, T value) where T : unmanaged
		{
			destination.Cast(out IPureOrMixedStorage? mixed, out ICachedStorage? cached);
			if (mixed is not null)
			{
				this.FromManaged(mixed[0], value);
			}
			else if (cached is not null)
			{
				var temp = cached.Retrieve(0, Storage<T>.SizeOfT);
				this.FromManaged(temp, value);
			}
		}

		/// <summary>
		/// When implemented by a derived class, copy out the first few elements in unmanaged pointer <paramref name="source"/> to a managed array of type <typeparamref name="T"/>.<br/>The default implementation only works for <see cref="Storage{T}.LocationDescription"/>.<see cref="CombinationOfLocations.Type">Type</see> == <see cref="CombinationType.PureOrMixed"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="source">The source <see cref="Storage{T}"/> to copy from</param>
		/// <param name="destination">The managed <see cref="ArraySegment{T}"/> of type <typeparamref name="T"/> to copy to</param>
		/// <returns>The number of elements (in <typeparamref name="T"/>) of actually copied block</returns>
		/// <exception cref="NotSupportedException">if <paramref name="source"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		public virtual long ToManaged<T>(Storage<T> source, ArraySegment<T> destination) where T : unmanaged
		{
			source.Cast(out IPureOrMixedStorage? mixed, out ICachedStorage? cached);
			if (mixed is not null)
			{
				int offset = 0;
				for (int i = 0; i < mixed.Count; i++)
				{
					this.ToManaged(mixed[i], destination[offset..]);
					if (offset >= destination.Count)
						break;
					offset += (int)(mixed[i].LengthInBytes / Storage<T>.SizeOfT);
				}
			}
			else if (cached is not null)
			{
				long count = Storage<T>.SizeOfT * (long)destination.Count;
				if (count >= cached.GetRealLength() / ICachedStorage.CacheSizeRatio)
				{
					cached.Flush();
					this.ToManaged(cached[0], destination);
				}
				else
				{
					cached.ApplyUnaryFunction(this.ToManaged, 0, count, auxiliaryOriginal: destination,
											  sliceFunc: static (dst, off, len) => dst.Slice((int)(off / Storage<T>.SizeOfT), (int)(len / Storage<T>.SizeOfT)),
											  copyFunc: this.MemoryCopy, pack: Storage<T>.SizeOfT);
				}
				return Math.Min(destination.Count, source.Length);
			}
			return Math.Min(source.Length, destination.Count);
		}

		/// <summary>
		/// When implemented by a derived class, overwrite the first few elements in unmanaged pointer <paramref name="destination"/> by the <paramref name="values"/> of a managed array of type <typeparamref name="T"/>.<br/>The default implementation only works for <see cref="Storage{T}.LocationDescription"/>.<see cref="CombinationOfLocations.Type">Type</see> == <see cref="CombinationType.PureOrMixed"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="destination">The destination <see cref="Storage{T}"/> to copy to</param>
		/// <param name="values">The managed <see cref="ArraySegment{T}"/> of type <typeparamref name="T"/> to copy from</param>
		/// <returns>The number of elements (in <typeparamref name="T"/>) of actually copied block</returns>
		/// <exception cref="NotSupportedException">if <paramref name="destination"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="destination"/> is invalid</exception>
		public virtual long FromManaged<T>(Storage<T> destination, ArraySegment<T> values) where T : unmanaged
		{
			destination.Cast(out IPureOrMixedStorage? mixed, out ICachedStorage? cached);
			if (mixed is not null)
			{
				int offset = 0;
				for (int i = 0; i < destination.Count; i++)
				{
					this.FromManaged(destination[i], values[offset..]);
					if (offset >= values.Count)
						break;
					offset += (int)(destination[i].LengthInBytes / Storage<T>.SizeOfT);
				}
			}
			else if (cached is not null)
			{
				long count = Storage<T>.SizeOfT * (long)values.Count;
				if (count >= cached.GetRealLength() / ICachedStorage.CacheSizeRatio)
				{
					cached.Flush();
					this.FromManaged(cached[0], values);
				}
				else
				{
					cached.ApplyUnaryFunction(this.FromManaged, 0, count, auxiliaryOriginal: values,
											  sliceFunc: static (src, off, len) => src.Slice((int)(off / Storage<T>.SizeOfT), (int)(len / Storage<T>.SizeOfT)),
											  copyFunc: this.MemoryCopy, pack: Storage<T>.SizeOfT);
				}
			}
			return Math.Min(destination.Length, values.Count);
		}

		/// <summary>
		/// When implemented by a derived class, copy out the elements in unmanaged pointer <paramref name="source"/> as a 2D matrix to a managed array of type <typeparamref name="T"/> (viewed as a 1D array).<br/>The default implementation only works for <see cref="Storage{T}.LocationDescription"/>.<see cref="CombinationOfLocations.Type">Type</see> == <see cref="CombinationType.PureOrMixed"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="source">The source <see cref="Storage{T}"/> to copy from</param>
		/// <param name="leadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="source"/></param>
		/// <param name="height">The height to copy in <typeparamref name="T"/></param>
		/// <param name="width">The width to copy in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="destination">The managed <see cref="ArraySegment{T}"/> of type <typeparamref name="T"/> to copy to, must has <c><see cref="Array.Length">Length</see> ¡Ý <paramref name="height"/> * <paramref name="width"/></c></param>
		/// <param name="destinationLeadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="destination"/>, default 0 means <paramref name="height"/></param>
		/// <exception cref="NotSupportedException">if <paramref name="source"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is invalid</exception>
		/// <exception cref="ArgumentException">
		/// If <paramref name="height"/> is larger than <paramref name="leadDim"/> or <paramref name="destinationLeadDim"/>,
		/// or <c><paramref name="leadDim"/> * <paramref name="width"/> &gt; <paramref name="source"/>.<see cref="Storage{T}.Length">Length</see></c>,
		/// or <c><paramref name="destinationLeadDim"/> * <paramref name="width"/> &gt; <paramref name="destination"/></c>.<see cref="Array.Length">Length</see>
		/// </exception>
		public virtual void ToManaged2D<T>(Storage<T> source, long leadDim, long height, long width, ArraySegment<T> destination, long destinationLeadDim = 0) where T : unmanaged
		{
			if (!source.IsValid())
				throw new ArgumentNullException(nameof(source));
			if (leadDim == 0)
				throw new ArgumentOutOfRangeException(nameof(leadDim), Resources.Parameter.MustPositive);
			if (width == 0)
				throw new ArgumentOutOfRangeException(nameof(width), Resources.Parameter.MustPositive);
			if (height == 0)
				throw new ArgumentOutOfRangeException(nameof(height), Resources.Parameter.MustPositive);
			if (height > leadDim || height > destinationLeadDim)
				throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(height));
			if (leadDim * width > source.Length)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(source));
			if (leadDim * width > destination.Count)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(destination));
			// shortcut
			if (source.Count == 1)
			{
				this.ToManaged2D(source[0], leadDim, height, width, destination, destinationLeadDim);
				return;
			}
			// normal case
			int h = checked((int)height), dstLD = checked((int)(destinationLeadDim == 0 ? height : destinationLeadDim));
			long srcLD = leadDim, max = leadDim * width;
			int dstOffset = 0;
			for (long srcOffset = 0; srcOffset < max; srcOffset += srcLD, dstOffset += dstLD)
			{
				var src = source.MakeReference(offset: srcOffset, newLength: height);
				var dst = destination.Slice(dstOffset, h);
				this.ToManaged(src, dst);
			}
		}

		/// <summary>
		/// When implemented by a derived class, overwrite some of the elements in unmanaged pointer <paramref name="destination"/> as a 2D matrix by  a managed array of type <typeparamref name="T"/> (viewed as a 1D array).<br/>The default implementation only works for <see cref="Storage{T}.LocationDescription"/>.<see cref="CombinationOfLocations.Type">Type</see> == <see cref="CombinationType.PureOrMixed"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="destination">The destination <see cref="Storage{T}"/> to copy to</param>
		/// <param name="leadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="destination"/></param>
		/// <param name="height">The height to copy in <typeparamref name="T"/></param>
		/// <param name="width">The width to copy in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="values">The managed <see cref="ArraySegment{T}"/> of type <typeparamref name="T"/> to copy from, must has <c><see cref="Array.Length">Length</see> ¡Ý <paramref name="height"/> * <paramref name="width"/></c></param>
		/// <param name="valuesLeadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="values"/>, default 0 means <paramref name="height"/></param>
		/// <exception cref="NotSupportedException">if <paramref name="destination"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="destination"/> is invalid</exception>
		/// <exception cref="ArgumentException">
		/// If <paramref name="height"/> is larger than <paramref name="leadDim"/> or <paramref name="valuesLeadDim"/>,
		/// or <c><paramref name="leadDim"/> * <paramref name="width"/> &gt; <paramref name="destination"/>.<see cref="Storage{T}.Length">Length</see></c>,
		/// or <c><paramref name="valuesLeadDim"/> * <paramref name="width"/> &gt; <paramref name="values"/></c>.<see cref="Array.Length">Length</see>
		/// </exception>
		public virtual void FromManaged2D<T>(Storage<T> destination, long leadDim, long height, long width, ArraySegment<T> values, long valuesLeadDim = 0) where T : unmanaged
		{
			if (!destination.IsValid())
				throw new ArgumentNullException(nameof(destination));
			if (leadDim == 0)
				throw new ArgumentOutOfRangeException(nameof(leadDim), Resources.Parameter.MustPositive);
			if (width == 0)
				throw new ArgumentOutOfRangeException(nameof(width), Resources.Parameter.MustPositive);
			if (height == 0)
				throw new ArgumentOutOfRangeException(nameof(height), Resources.Parameter.MustPositive);
			if (height > leadDim || height > valuesLeadDim)
				throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(height));
			if (leadDim * width > destination.Length)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(destination));
			if (leadDim * width > values.Count)
				throw new ArgumentException(Resources.Parameter.WrongSize, nameof(values));
			// shortcut
			if (destination.Count == 1)
			{
				this.FromManaged2D(destination[0], leadDim, height, width, values);
				return;
			}
			// normal case
			int h = checked((int)height), srcLD = checked((int)(valuesLeadDim == 0 ? height : valuesLeadDim));
			long dstLD = leadDim, max = leadDim * width;
			int srcOffset = 0;
			for (long dstOffset = 0; dstOffset < max; dstOffset += dstLD, srcOffset += srcLD)
			{
				var dst = destination.MakeReference(offset: dstOffset, newLength: height);
				var src = values.Slice(srcOffset, h);
				this.FromManaged(dst, src);
			}
		}
		#endregion
	}
}