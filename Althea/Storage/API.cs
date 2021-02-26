using System;
using System.Dynamic;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.Resources;
using Althea.NativeTypes;


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
		private static bool StaticCopy(PointerSegment source, PointerSegment destination, out long copied)
		{
			try
			{
				copied = AbstractApi.MemoryCopy(source, destination);
				return true;
			}
			catch (System.Exception)
			{
				copied = 0;
				return false;
			}
		}

		/// <summary>
		/// Copy memory from <paramref name="source"/> to <paramref name="destination"/> where both are <see cref="IPureOrMixedStorage"/>.
		/// </summary>
		/// <param name="source">The source <see cref="IPureOrMixedStorage"/> to copy from</param>
		/// <param name="destination">The destination <see cref="IPureOrMixedStorage"/> to copy into</param>
		/// <param name="copyFunc">The <see cref="ICachedStorage.CopyDelegate"/> used to copy data, default is the <see cref="AbstractApi.MemoryCopy(PointerSegment, PointerSegment)"/></param>
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

			copyFunc ??= StaticCopy;

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
				copyFunc.Invoke(src, dst, out long copyCount);
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
		/// <param name="blockCopy">The <see cref="ICachedStorage.CopyDelegate"/> used to copy data and invoke <see cref="ICachedStorage.Retrieve(long, long, ICachedStorage.CopyDelegate?)"/>, default is the <see cref="AbstractApi.MemoryCopy(PointerSegment, PointerSegment)"/></param>
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

			blockCopy ??= StaticCopy;
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
					alignedCopy.Invoke(src, dst, out long copiedCount);
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
		/// <param name="blockCopy">The <see cref="ICachedStorage.CopyDelegate"/> used to copy data and invoke <see cref="ICachedStorage.Retrieve(long, long, ICachedStorage.CopyDelegate?)"/>, default is the <see cref="AbstractApi.MemoryCopy(PointerSegment, PointerSegment)"/></param>
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

			blockCopy ??= StaticCopy;
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
					alignedCopy.Invoke(src, dst, out long copiedCount);
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
		/// <param name="blockCopy">The <see cref="ICachedStorage.CopyDelegate"/> used to copy data and invoke <see cref="ICachedStorage.Retrieve(long, long, ICachedStorage.CopyDelegate?)"/>, default is the <see cref="AbstractApi.MemoryCopy(PointerSegment, PointerSegment)"/></param>
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

			blockCopy ??= StaticCopy;
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
				alignedCopy.Invoke(src, dst, out long copiedCount);
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
		#region basic
		/// <summary>
		/// Get the current using <see cref="AbstractApi"/>.
		/// </summary>
		/// <remarks><b>DO NOT</b> invoke methods of this property directly unless you are sure about what you are doing; otherwise, there may be exceptions and / or unnoticeable bugs.</remarks>
		public static AbstractApi? Current => RecentAPIs.First?.Value;

		private static readonly LinkedList<AbstractApi> RecentAPIs = new LinkedList<AbstractApi>();

		internal static bool SetImplementation(Type implementation) => SetImplementation(RecentAPIs, implementation);
		#endregion


		#region dynamic invocation
		/// <summary>
		/// Get the dynamic object used to dynamically invoke method(s) not listed explicitly here (the methods extra defined in derived classes)
		/// </summary>
		/// <remarks>
		/// Due to the limitations of dynamic invocation, <c>ref</c>, <c>in</c>, <c>out</c> and <c>ref struct</c>, etc. are not supported and non of the input arguments can be null.<br/>
		/// Since there are internal caching for <see cref="DynamicObject.TryInvokeMember(InvokeMemberBinder, object[], out object)"/>, the average repeated dynamic invocation may cost around 1 microsecond.
		/// </remarks>
		/// <example><code>
		/// long actualCopied = AbstractApi.Dynamic.MemoryCopyPitched(...);
		/// </code></example>
		public static dynamic Dynamic => singletonDynamic;

		private static readonly DynamicInvocations singletonDynamic = new DynamicInvocations();

		private sealed class DynamicInvocations : DynamicInvocation
		{
			public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
			{
				result = DynamicInvokeExtraMethod(RecentAPIs, binder.Name, args);
				return true;
			}
		}
		#endregion


		#region support information
		/// <summary>
		/// When implemented by a derived class, check if the given <paramref name="location"/> is supported by unary operations of this implementation or not.
		/// </summary>
		/// <param name="location">The given <see cref="CombinationOfLocations"/></param>
		/// <returns>Whether <paramref name="location"/> is supported by this <see cref="AbstractApi"/>.</returns>
		/// <remarks>
		/// The unary operations:
		/// <list type="bullet">
		/// <item><see cref="Allocate(StorageLocation, long)"/></item>
		/// <item><see cref="Free(PointerSegment, bool)"/></item>
		/// <item><see cref="FillWithValue(PointerSegment, byte)"/></item>
		/// <item>etc.</item>
		/// </list>
		/// </remarks>
		protected abstract bool IsSupportedUnary(CombinationOfLocations location);

		/// <summary>
		/// When implemented by a derived class, check if the given <see cref="CombinationOfLocations"/>s are supported by binary operations of this implementation or not.
		/// </summary>
		/// <param name="location1">The first given <see cref="CombinationOfLocations"/></param>
		/// <param name="location2">The second given <see cref="CombinationOfLocations"/></param>
		/// <returns>Whether binary operations between <paramref name="location1"/> and <paramref name="location2"/> are supported by this <see cref="AbstractApi"/>.</returns>
		/// <remarks>
		/// The binary operations:
		/// <list type="bullet">
		/// <item><see cref="MemoryCopy(PointerSegment, PointerSegment)"/></item>
		/// <item><see cref="MemoryCopy2D(PointerSegment, long, PointerSegment, long, long, long)"/></item>
		/// <item>etc.</item>
		/// </list>
		/// </remarks>
		protected abstract bool IsSupportedBinary(CombinationOfLocations location1, CombinationOfLocations location2);

		/// <summary>
		/// When implemented by a derived class, check whether the given <see cref="CombinationOfLocations"/> can transfer data with C# managed memory using this implementation or not.
		/// </summary>
		/// <param name="location">The <see cref="CombinationOfLocations"/> to indicate the unmanaged storage location combination</param>
		/// <returns>Whether this <see cref="AbstractApi"/> supports data transfer between <paramref name="location"/> and C# managed memory</returns>
		/// <remarks>
		/// The transfer operations:
		/// <list type="bullet">
		/// <item><see cref="FromManaged{T}(PointerSegment, T)"/></item>
		/// <item><see cref="ToManaged{T}(PointerSegment)"/></item>
		/// <item>etc.</item>
		/// </list>
		/// </remarks>
		protected abstract bool CanTransferWithManaged(CombinationOfLocations location);
		#endregion


		#region properties
		/// <summary>
		/// When implemented by a derived class, check if the given <paramref name="location"/> is supported by the underlying driver of this implementation or not.
		/// </summary>
		/// <param name="location">The given <see cref="StorageLocation"/> to check</param>
		/// <returns>Whether <paramref name="location"/> is supported by this <see cref="AbstractApi"/></returns>
		public abstract bool IsSupportedLocation(StorageLocation location);

		/// <summary>
		/// When implemented by a derived class, get the underlying driver's version of a supported <see cref="StorageLocation"/>.
		/// </summary>
		/// <param name="location">The given supported <see cref="StorageLocation"/></param>
		/// <returns>The underlying driver's major version of <paramref name="location"/></returns>
		public abstract (int major, int minor) DriverVersion(StorageLocation location);

		/// <summary>
		/// When implemented by a derived class, get the available and total memory in bytes for device indicated by a supported <see cref="StorageLocation"/>.
		/// </summary>
		/// <param name="location">The given supported <see cref="StorageLocation"/></param>
		/// <returns>The free and total memory space in bytes of <paramref name="location"/></returns>
		public abstract (long free, long total) FreeAndTotalMemory(StorageLocation location);
		#endregion


		#region static methods as dispatchers
		#region low-level storage operations
		/// <summary>
		/// Allocate a storage at given <paramref name="location"/> with given <paramref name="length"/> in bytes
		/// </summary>
		/// <param name="location">The <see cref="StorageLocation"/> to allocate on</param>
		/// <param name="length">The length to allocate in bytes</param>
		/// <returns>The allocated pointer as a <see cref="PointerSegment"/></returns>
		/// <exception cref="AggregateException">If this implementation cannot allocate <paramref name="length"/> on <paramref name="location"/> due to other issues such as <see cref="OutOfMemoryException"/></exception>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		protected internal static PointerSegment Allocate(StorageLocation location, long length)
		{
			PointerSegment result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedLocation(location), node);
				success = node.Value.Allocate_(location, length, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// Allocate a storage at given <paramref name="location"/> with given <paramref name="length"/> in <typeparamref name="T"/>. The implementation utilizes <see cref="Allocate(StorageLocation, long)"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="location">The <see cref="StorageLocation"/> to allocate on</param>
		/// <param name="length">The length to allocate in <typeparamref name="T"/> rather than bytes</param>
		/// <returns>The allocated pointer as a <see cref="PointerSegment"/></returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="AggregateException">If this implementation cannot allocate <paramref name="length"/> on <paramref name="location"/> due to other issues such as <see cref="OutOfMemoryException"/></exception>
		protected internal static PointerSegment Allocate<T>(StorageLocation location, long length) where T : unmanaged
		{
			return Allocate(location, length * Storage<T>.SizeOfT);
		}

		/// <summary>
		/// Free a storage indicated by a given <paramref name="pointer"/>
		/// </summary>
		/// <param name="pointer">The <see cref="PointerSegment"/> to free</param>
		/// <param name="disposeManaged">Whether to dispose managed resources held by <paramref name="pointer"/>'s <see cref="PointerSegment.Pointer"/> or not</param>
		/// <returns>True if <paramref name="pointer"/> is valid the free succeeded; false otherwise.</returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		protected internal static bool Free(PointerSegment pointer, bool disposeManaged = true)
		{
			StorageLocation location = pointer.Location;
			bool result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedLocation(location), node);
				success = node.Value.Free_(pointer, disposeManaged, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// Fill the <paramref name="pointer"/> by same <paramref name="value"/>, byte by byte.
		/// </summary>
		/// <param name="pointer">The pointer to be filled</param>
		/// <param name="value">The value to set as a <see cref="byte"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="pointer"/> is invalid</exception>
		public static void FillWithValue(PointerSegment pointer, byte value)
		{
			StorageLocation location = pointer.Location;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedLocation(location), node);
				success = node.Value.FillWithValue_(pointer, value);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Fill the <paramref name="pointer"/>'s each value by same <paramref name="value"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="pointer">The pointer to be filled</param>
		/// <param name="value">The value to set as a <typeparamref name="T"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="pointer"/> is invalid</exception>
		public static void FillWithValue<T>(PointerSegment pointer, T value) where T : unmanaged
		{
			StorageLocation location = pointer.Location;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedLocation(location), node);
				success = node.Value.FillWithValue_(pointer, value);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Copy memory from <paramref name="source"/> to <paramref name="destination"/>.
		/// </summary>
		/// <param name="source">The source pointer to copy from</param>
		/// <param name="destination">The destination pointer to copy into</param>
		/// <returns>The number of bytes of actually copied block</returns>
		/// <remarks>The one with less length in <paramref name="source"/> and <paramref name="destination"/> is used as the actual copy length in bytes</remarks>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="source"/> or <paramref name="destination"/> is invalid</exception>
		public static long MemoryCopy(PointerSegment source, PointerSegment destination)
		{
			CombinationOfLocations src = source.Location, dst = destination.Location;
			long result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedBinary(src, dst), node);
				success = node.Value.MemoryCopy_(source, destination, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// Copy 2D data from <paramref name="source"/> to <paramref name="destination"/>.
		/// </summary>
		/// <param name="source">The source pointer</param>
		/// <param name="sourceLD">The source array actual height (actual leading dimension) in bytes</param>
		/// <param name="destination">The destination pointer</param>
		/// <param name="destinationLD">The destination array actual height (actual leading dimension) in bytes</param>
		/// <param name="height">The height to copy in bytes</param>
		/// <param name="width">The width to copy in the real type</param>
		/// <remarks>The lengths of <paramref name="source"/> and <paramref name="destination"/> are ignored</remarks>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentOutOfRangeException">If any of the parameters is zero</exception>
		/// <exception cref="NullReferenceException">If <paramref name="source"/> or <paramref name="destination"/> is invalid</exception>
		/// <exception cref="ArgumentException">
		/// If <paramref name="height"/> is larger than <paramref name="sourceLD"/> or <paramref name="destinationLD"/>,
		/// or <paramref name="height"/> is larger than <paramref name="sourceLD"/> or <paramref name="destinationLD"/>,
		/// or <c><paramref name="sourceLD"/> * <paramref name="width"/> &gt; <paramref name="source"/>.<see cref="IStorage.LengthInBytes">Length</see></c>, 
		/// or <c><paramref name="destinationLD"/> * <paramref name="width"/> &gt; <paramref name="destination"/>.<see cref="IStorage.LengthInBytes">Length</see></c>
		/// </exception>
		public static void MemoryCopy2D(PointerSegment source, long sourceLD, PointerSegment destination, long destinationLD, long height, long width)
		{
			CombinationOfLocations src = source.Location, dst = destination.Location;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedBinary(src, dst), node);
				success = node.Value.MemoryCopy2D_(source, sourceLD, destination, destinationLD, height, width);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Copy the <paramref name="source"/> storage to <paramref name="destination"/> storage with given strides.<br/>
		/// <c><paramref name="destination"/>[j] = <paramref name="source"/>[k] for i = 1,¡­,n; k = 1 + (i - 1)*<paramref name="incrementSource"/>, j = 1 + (i - 1)*<paramref name="incrementDestination"/></c>.<br/>
		/// The number of elements copied is calculated to the maximum possible value that does not exceeds the boundaries.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="source">The <see cref="PointerSegment"/> to copy from</param>
		/// <param name="incrementSource">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="source"/></param>
		/// <param name="destination">The <see cref="PointerSegment"/> to copy to</param>
		/// <param name="incrementDestination">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="destination"/></param>
		/// <returns>The number of elements (in <typeparamref name="T"/>) actually copied</returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="source"/> or <paramref name="destination"/>is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="incrementSource"/> or <paramref name="incrementDestination"/> is less than 1</exception>
		public static long StridedCopy<T>(PointerSegment source, int incrementSource, PointerSegment destination, int incrementDestination) where T : unmanaged
		{
			CombinationOfLocations src = source.Location, dst = destination.Location;
			long result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedBinary(src, dst), node);
				success = node.Value.StridedCopy_<T>(source, incrementSource, destination, incrementDestination, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}
		#endregion

		#region low-level storage and managed operations
		/// <summary>
		/// Copy out the <b>first</b> element in unmanaged pointer <paramref name="source"/> to a managed value of type <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="source">The source <see cref="PointerSegment"/> to copy from</param>
		/// <returns>The first element in <paramref name="source"/></returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="source"/> is invalid</exception>
		public static T ToManaged<T>(PointerSegment source) where T : unmanaged
		{
			CombinationOfLocations location = source.Location;
			T result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.CanTransferWithManaged(location), node);
				success = node.Value.ToManaged_(source, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// Overwrite the <b>first</b> element in unmanaged pointer <paramref name="destination"/> by a managed <paramref name="value"/> of type <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="destination">The destination <see cref="PointerSegment"/> to copy to</param>
		/// <param name="value">The value of type <typeparamref name="T"/> to copy from</param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="destination"/> is invalid</exception>
		public static void FromManaged<T>(PointerSegment destination, T value) where T : unmanaged
		{
			CombinationOfLocations location = destination.Location;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.CanTransferWithManaged(location), node);
				success = node.Value.FromManaged_(destination, value);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Copy out the first few elements in unmanaged pointer <paramref name="source"/> to a managed array of type <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="source">The source <see cref="PointerSegment"/> to copy from</param>
		/// <param name="destination">The managed <see cref="Span{T}"/> of type <typeparamref name="T"/> to copy to</param>
		/// <return>The number of elements (in <typeparamref name="T"/>) actually copied</return>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="source"/> is invalid</exception>
		public static long ToManaged<T>(PointerSegment source, Span<T> destination) where T : unmanaged
		{
			CombinationOfLocations location = source.Location;
			long result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.CanTransferWithManaged(location), node);
				success = node.Value.ToManaged_(source, destination, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// Overwrite the first few elements in unmanaged pointer <paramref name="destination"/> by the <paramref name="values"/> of a managed array of type <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="destination">The destination <see cref="PointerSegment"/> to copy to</param>
		/// <param name="values">The managed <see cref="ReadOnlySpan{T}"/> of type <typeparamref name="T"/> to copy from</param>
		/// <return>The number of elements (in <typeparamref name="T"/>) actually copied</return>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="destination"/> is invalid</exception>
		public static long FromManaged<T>(PointerSegment destination, ReadOnlySpan<T> values) where T : unmanaged
		{
			CombinationOfLocations location = destination.Location;
			long result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.CanTransferWithManaged(location), node);
				success = node.Value.FromManaged_(destination, values, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		// Ignore Spelling: sizeof
		/// <summary>
		/// Copy out the elements in unmanaged pointer <paramref name="source"/> as a 2D matrix to a managed array of type <typeparamref name="T"/> (viewed as a 1D array).
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="source">The source <see cref="PointerSegment"/> to copy from</param>
		/// <param name="leadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="source"/></param>
		/// <param name="height">The height to copy in <typeparamref name="T"/></param>
		/// <param name="width">The width to copy in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="destination">The managed <see cref="Span{T}"/> of type <typeparamref name="T"/> to copy to, must has <c><see cref="Array.Length">Length</see> ¡Ý <paramref name="height"/> <paramref name="width"/></c></param>
		/// <param name="destinationLeadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="destination"/>, default 0 means <paramref name="height"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="source"/> is invalid</exception>
		/// <exception cref="ArgumentException">
		/// If <paramref name="height"/> is larger than <paramref name="leadDim"/> or <paramref name="destinationLeadDim"/>,
		/// or <c><paramref name="leadDim"/> * <paramref name="width"/> * sizeof(<typeparamref name="T"/>) &gt; <paramref name="source"/>.<see cref="PointerSegment.LengthInBytes">Length</see></c>,
		/// or <c><paramref name="destinationLeadDim"/> * <paramref name="width"/> &gt; <paramref name="destination"/></c>.<see cref="Array.Length">Length</see>
		/// </exception>
		public static void ToManaged2D<T>(PointerSegment source, long leadDim, long height, long width, Span<T> destination, long destinationLeadDim = 0) where T : unmanaged
		{
			CombinationOfLocations location = source.Location;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.CanTransferWithManaged(location), node);
				success = node.Value.ToManaged2D_(source, leadDim, height, width, destination, destinationLeadDim);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Overwrite some of the elements in unmanaged pointer <paramref name="destination"/> as a 2D matrix by  a managed array of type <typeparamref name="T"/> (viewed as a 1D array).
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="destination">The destination <see cref="PointerSegment"/> to copy to</param>
		/// <param name="leadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="destination"/></param>
		/// <param name="height">The height to copy in <typeparamref name="T"/></param>
		/// <param name="width">The width to copy in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="values">The managed <see cref="ReadOnlySpan{T}"/> of type <typeparamref name="T"/> to copy from, must has <c><see cref="Array.Length">Length</see> ¡Ý <paramref name="height"/> * <paramref name="width"/></c></param>
		/// <param name="valuesLeadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="values"/>, default 0 means <paramref name="height"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="destination"/> is invalid</exception>
		/// <exception cref="ArgumentException">
		/// If <paramref name="height"/> is larger than <paramref name="leadDim"/> or <paramref name="valuesLeadDim"/>,
		/// or <c><paramref name="leadDim"/> * <paramref name="width"/> * sizeof(<typeparamref name="T"/>) &gt; <paramref name="destination"/>.<see cref="PointerSegment.LengthInBytes">Length</see></c>,
		/// or <c><paramref name="valuesLeadDim"/> * <paramref name="width"/> &gt; <paramref name="values"/></c>.<see cref="Array.Length">Length</see>
		/// </exception>
		public static void FromManaged2D<T>(PointerSegment destination, long leadDim, long height, long width, ReadOnlySpan<T> values, long valuesLeadDim = 0) where T : unmanaged
		{
			CombinationOfLocations location = destination.Location;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.CanTransferWithManaged(location), node);
				success = node.Value.FromManaged2D_(destination, leadDim, height, width, values, valuesLeadDim);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}
		#endregion

		#region high-level storage operations
		/// <summary>
		/// Fill the <paramref name="storage"/> byte by byte to the same <paramref name="value"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="storage">The <see cref="Storage{T}"/> to be filled</param>
		/// <param name="value">The value to fill</param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="storage"/> is null or invalid</exception>
		public static void FillWithValue<T>(Storage<T> storage, byte value) where T : unmanaged
		{
			CombinationOfLocations location = storage.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedUnary(location));
				success = node.Value.FillWithValue_(storage, value);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Fill the <paramref name="storage"/>'s each value by same <paramref name="value"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="storage">The <see cref="Storage{T}"/> to be filled</param>
		/// <param name="value">The value to fill</param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="storage"/> is null or invalid</exception>
		public static void FillWithValue<T>(Storage<T> storage, T value) where T : unmanaged, IEquatable<T>
		{
			CombinationOfLocations location = storage.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedUnary(location));
				success = node.Value.FillWithValue_(storage, value);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Copy memory from <paramref name="source"/> to <paramref name="destination"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="source">The source <see cref="Storage{T}"/> to copy from</param>
		/// <param name="destination">The destination <see cref="Storage{T}"/> pointer to copy into</param>
		/// <return>The number of elements (in <typeparamref name="T"/>) actually copied</return>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <remarks>The one with less length among <paramref name="source"/> and <paramref name="destination"/> is used as the actual copy length</remarks>
		/// <exception cref="NotSupportedException">If <paramref name="source"/> or <paramref name="destination"/> is not supported</exception>
		/// <exception cref="NullReferenceException">If <paramref name="source"/> or <paramref name="destination"/> is null or invalid</exception>
		public static long MemoryCopy<T>(Storage<T> source, Storage<T> destination) where T : unmanaged
		{
			CombinationOfLocations src = source.LocationDescription, dst = destination.LocationDescription;
			long result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedBinary(src, dst), node);
				success = node.Value.MemoryCopy_(source, destination, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// Copy the <paramref name="source"/> storage to <paramref name="destination"/> storage with given strides.<br/>
		/// <c><paramref name="destination"/>[j] = <paramref name="source"/>[k] for i = 1,¡­,n; k = 1 + (i - 1)*<paramref name="incrementSource"/>, j = 1 + (i - 1)*<paramref name="incrementDestination"/></c>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="source">The <see cref="Storage{T}"/> to copy from</param>
		/// <param name="incrementSource">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="source"/></param>
		/// <param name="destination">The <see cref="Storage{T}"/> to copy to</param>
		/// <param name="incrementDestination">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="destination"/></param>
		/// <return>The number of elements (in <typeparamref name="T"/>) actually copied</return>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="source"/> or <paramref name="destination"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="incrementSource"/> or <paramref name="incrementDestination"/> is less than 1</exception>
		public static long StridedCopy<T>(Storage<T> source, int incrementSource, Storage<T> destination, int incrementDestination) where T : unmanaged
		{
			CombinationOfLocations src = source.LocationDescription, dst = destination.LocationDescription;
			long result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedBinary(src, dst), node);
				success = node.Value.StridedCopy_(source, incrementSource, destination, incrementDestination, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// Copy 2D data from <paramref name="source"/> to <paramref name="destination"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="source">The source <see cref="Storage{T}"/></param>
		/// <param name="sourceLD">The source array actual height (actual leading dimension) in <typeparamref name="T"/></param>
		/// <param name="destination">The destination <see cref="Storage{T}"/></param>
		/// <param name="destLD">The destination array actual height (actual leading dimension) in <typeparamref name="T"/></param>
		/// <param name="height">The height to copy in <typeparamref name="T"/></param>
		/// <param name="width">The width to copy in the real type</param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <remarks>The lengths of <paramref name="source"/> and <paramref name="destination"/> are <b>not</b> ignored</remarks>
		/// <exception cref="NullReferenceException">If <paramref name="source"/> or <paramref name="destination"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If any of the parameters is zero</exception>
		/// <exception cref="ArgumentException">
		/// If <paramref name="height"/> is larger than <paramref name="sourceLD"/> or <paramref name="destLD"/>,
		/// or <c><paramref name="sourceLD"/> * <paramref name="width"/> &gt; <paramref name="source"/>.<see cref="Storage{T}.Length">Length</see></c>, 
		/// or <c><paramref name="destLD"/> * <paramref name="width"/> &gt; <paramref name="destination"/>.<see cref="Storage{T}.Length">Length</see></c>
		/// </exception>
		public static void MemoryCopy2D<T>(Storage<T> source, long sourceLD, Storage<T> destination, long destLD, long height, long width) where T : unmanaged
		{
			CombinationOfLocations src = source.LocationDescription, dst = destination.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.IsSupportedBinary(src, dst), node);
				success = node.Value.MemoryCopy2D_(source, sourceLD, destination, destLD, height, width);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}
		#endregion

		#region high-level storage and managed operations
		/// <summary>
		/// Copy out the <b>first</b> element in <see cref="Storage{T}"/> <paramref name="source"/> to a managed value of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="source">The source <see cref="Storage{T}"/> to copy from</param>
		/// <returns>The first element in <paramref name="source"/></returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="source"/> is null or invalid</exception>
		public static T ToManaged<T>(Storage<T> source) where T : unmanaged
		{
			CombinationOfLocations location = source.LocationDescription;
			T result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.CanTransferWithManaged(location), node);
				success = node.Value.ToManaged_(source, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// Overwrite the <b>first</b> element in unmanaged pointer <paramref name="destination"/> by a managed <paramref name="value"/> of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="destination">The destination <see cref="Storage{T}"/> to copy to</param>
		/// <param name="value">The value of type <typeparamref name="T"/> to copy from</param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="destination"/> is null or invalid</exception>
		public static void FromManaged<T>(Storage<T> destination, T value) where T : unmanaged
		{
			CombinationOfLocations location = destination.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.CanTransferWithManaged(location), node);
				success = node.Value.FromManaged_(destination, value);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Copy out the first few elements in unmanaged pointer <paramref name="source"/> to a managed array of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="source">The source <see cref="Storage{T}"/> to copy from</param>
		/// <param name="destination">The managed <see cref="Span{T}"/> of type <typeparamref name="T"/> to copy to</param>
		/// <returns>The number of elements (in <typeparamref name="T"/>) actually copied</returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="source"/> is null or invalid</exception>
		public static long ToManaged<T>(Storage<T> source, Span<T> destination) where T : unmanaged
		{
			CombinationOfLocations location = source.LocationDescription;
			long result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.CanTransferWithManaged(location), node);
				success = node.Value.ToManaged_(source, destination, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// Overwrite the first few elements in unmanaged pointer <paramref name="destination"/> by the <paramref name="values"/> of a managed array of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="destination">The destination <see cref="Storage{T}"/> to copy to</param>
		/// <param name="values">The managed <see cref="ReadOnlySpan{T}"/> of type <typeparamref name="T"/> to copy from</param>
		/// <returns>The number of elements (in <typeparamref name="T"/>) actually copied</returns>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="destination"/> is invalid</exception>
		public static long FromManaged<T>(Storage<T> destination, ReadOnlySpan<T> values) where T : unmanaged
		{
			CombinationOfLocations location = destination.LocationDescription;
			long result = default;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.CanTransferWithManaged(location), node);
				success = node.Value.FromManaged_(destination, values, out result);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			return result;
		}

		/// <summary>
		/// Copy out the elements in unmanaged pointer <paramref name="source"/> as a 2D matrix to a managed array of type <typeparamref name="T"/> (viewed as a 1D array).
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="source">The source <see cref="Storage{T}"/> to copy from</param>
		/// <param name="leadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="source"/></param>
		/// <param name="height">The height to copy in <typeparamref name="T"/></param>
		/// <param name="width">The width to copy in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="destination">The managed <see cref="Span{T}"/> of type <typeparamref name="T"/> to copy to, must has <c><see cref="Array.Length">Length</see> ¡Ý <paramref name="height"/> * <paramref name="width"/></c></param>
		/// <param name="destinationLeadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="destination"/>, default 0 means <paramref name="height"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="source"/> is invalid</exception>
		/// <exception cref="ArgumentException">
		/// If <paramref name="height"/> is larger than <paramref name="leadDim"/> or <paramref name="destinationLeadDim"/>,
		/// or <c><paramref name="leadDim"/> * <paramref name="width"/> &gt; <paramref name="source"/>.<see cref="Storage{T}.Length">Length</see></c>,
		/// or <c><paramref name="destinationLeadDim"/> * <paramref name="width"/> &gt; <paramref name="destination"/></c>.<see cref="Array.Length">Length</see>
		/// </exception>
		public static void ToManaged2D<T>(Storage<T> source, long leadDim, long height, long width, Span<T> destination, long destinationLeadDim = 0) where T : unmanaged
		{
			CombinationOfLocations location = source.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.CanTransferWithManaged(location), node);
				success = node.Value.ToManaged2D_(source, leadDim, height, width, destination, destinationLeadDim);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Overwrite some of the elements in unmanaged pointer <paramref name="destination"/> as a 2D matrix by  a managed array of type <typeparamref name="T"/> (viewed as a 1D array).
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="destination">The destination <see cref="Storage{T}"/> to copy to</param>
		/// <param name="leadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="destination"/></param>
		/// <param name="height">The height to copy in <typeparamref name="T"/></param>
		/// <param name="width">The width to copy in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="values">The managed <see cref="ReadOnlySpan{T}"/> of type <typeparamref name="T"/> to copy from, must has <c><see cref="Array.Length">Length</see> ¡Ý <paramref name="height"/> * <paramref name="width"/></c></param>
		/// <param name="valuesLeadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="values"/>, default 0 means <paramref name="height"/></param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="NullReferenceException">If <paramref name="destination"/> is invalid</exception>
		/// <exception cref="ArgumentException">
		/// If <paramref name="height"/> is larger than <paramref name="leadDim"/> or <paramref name="valuesLeadDim"/>,
		/// or <c><paramref name="leadDim"/> * <paramref name="width"/> &gt; <paramref name="destination"/>.<see cref="Storage{T}.Length">Length</see></c>,
		/// or <c><paramref name="valuesLeadDim"/> * <paramref name="width"/> &gt; <paramref name="values"/></c>.<see cref="Array.Length">Length</see>
		/// </exception>
		public static void FromManaged2D<T>(Storage<T> destination, long leadDim, long height, long width, ReadOnlySpan<T> values, long valuesLeadDim = 0) where T : unmanaged
		{
			CombinationOfLocations location = destination.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(RecentAPIs, a => a.CanTransferWithManaged(location), node);
				success = node.Value.FromManaged2D_(destination, leadDim, height, width, values, valuesLeadDim);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}
		#endregion
		#endregion


		#region abstract methods that actually do computations
		#region low-level storage operations
		/// <summary>
		/// When implemented by a derived class, allocate a storage at given <paramref name="location"/> with given <paramref name="length"/> in bytes
		/// </summary>
		/// <param name="location">The <see cref="StorageLocation"/> to allocate on</param>
		/// <param name="length">The length to allocate in bytes</param>
		/// <param name="result">The result -- an allocated pointer as a <see cref="PointerSegment"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="AggregateException">If this implementation cannot allocate <paramref name="length"/> on <paramref name="location"/> due to other issues such as <see cref="OutOfMemoryException"/></exception>
		/// <remarks>This methods shall <b>never</b> be exposed publicly to prevent unexpected memory leaks which GC cannot collect due to improper usage.</remarks>
		protected abstract bool Allocate_(StorageLocation location, long length, out PointerSegment result);

		/// <summary>
		/// When implemented by a derived class, free a storage indicated by a given <paramref name="pointer"/>
		/// </summary>
		/// <param name="pointer">The <see cref="PointerSegment"/> to free</param>
		/// <param name="disposeManaged">Whether to dispose managed resources held by <paramref name="pointer"/>'s <see cref="PointerSegment.Pointer"/> or not</param>
		/// <param name="valid">If <paramref name="pointer"/> is not valid, output false; otherwise, output true</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <remarks>This methods shall <b>never</b> be exposed publicly to prevent unexpected wild pointers due to improper usage.</remarks>
		protected abstract bool Free_(PointerSegment pointer, bool disposeManaged, out bool valid);

		/// <summary>
		/// When implemented by a derived class, fill the <paramref name="pointer"/> by same <paramref name="value"/>, byte by byte.
		/// </summary>
		/// <param name="pointer">The pointer to be filled</param>
		/// <param name="value">The value to set as a <see cref="byte"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pointer"/> is invalid</exception>
		protected abstract bool FillWithValue_(PointerSegment pointer, byte value);

		/// <summary>
		/// When implemented by a derived class, fill the <paramref name="pointer"/>'s each value by same <paramref name="value"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="pointer">The pointer to be filled</param>
		/// <param name="value">The value to set as a <typeparamref name="T"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pointer"/> is invalid</exception>
		protected abstract bool FillWithValue_<T>(PointerSegment pointer, T value) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, copy memory from <paramref name="source"/> to <paramref name="destination"/>.
		/// </summary>
		/// <param name="source">The source pointer to copy from</param>
		/// <param name="destination">The destination pointer to copy into</param>
		/// <param name="actualCopied">Output the number of bytes of actually copied block</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <remarks>The one with less length in <paramref name="source"/> and <paramref name="destination"/> is used as the actual copy length in bytes</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is invalid</exception>
		protected abstract bool MemoryCopy_(PointerSegment source, PointerSegment destination, out long actualCopied);

		/// <summary>
		/// When implemented by a derived class, copy 2D data from <paramref name="source"/> to <paramref name="destination"/>.
		/// </summary>
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
		protected abstract bool MemoryCopy2D_(PointerSegment source, long sourceLD, PointerSegment destination, long destinationLD, long height, long width);

		/// <summary>
		/// When implemented by a derived class, copy the <paramref name="source"/> storage to <paramref name="destination"/> storage with given strides.<br/>
		/// <c><paramref name="destination"/>[j] = <paramref name="source"/>[k] for i = 1,¡­,n; k = 1 + (i - 1)*<paramref name="incrementSource"/>, j = 1 + (i - 1)*<paramref name="incrementDestination"/></c>.<br/>
		/// The number of elements copied is calculated to the maximum possible value that does not exceeds the boundaries.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="source">The <see cref="PointerSegment"/> to copy from</param>
		/// <param name="incrementSource">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="source"/></param>
		/// <param name="destination">The <see cref="PointerSegment"/> to copy to</param>
		/// <param name="incrementDestination">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="destination"/></param>
		/// <param name="actualCopied">Output the number of elements (in <typeparamref name="T"/>) actually copied</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/>is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="incrementSource"/> or <paramref name="incrementDestination"/> is less than 1</exception>
		protected abstract bool StridedCopy_<T>(PointerSegment source, int incrementSource, PointerSegment destination, int incrementDestination, out long actualCopied) where T : unmanaged;
		#endregion

		#region low-level storage and managed operations
		/// <summary>
		/// When implemented by a derived class, copy out the <b>first</b> element in unmanaged pointer <paramref name="source"/> to a managed value of type <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="source">The source <see cref="PointerSegment"/> to copy from</param>
		/// <param name="value">Output the first element in <paramref name="source"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is invalid</exception>
		protected abstract bool ToManaged_<T>(PointerSegment source, out T value) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, overwrite the <b>first</b> element in unmanaged pointer <paramref name="destination"/> by a managed <paramref name="value"/> of type <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="destination">The destination <see cref="PointerSegment"/> to copy to</param>
		/// <param name="value">The value of type <typeparamref name="T"/> to copy from</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="destination"/> is invalid</exception>
		protected abstract bool FromManaged_<T>(PointerSegment destination, T value) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, copy out the first few elements in unmanaged pointer <paramref name="source"/> to a managed array of type <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="source">The source <see cref="PointerSegment"/> to copy from</param>
		/// <param name="destination">The managed <see cref="Span{T}"/> of type <typeparamref name="T"/> to copy to</param>
		/// <param name="actualCopied">Output the number of elements (in <typeparamref name="T"/>) actually copied</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is invalid</exception>
		protected abstract bool ToManaged_<T>(PointerSegment source, Span<T> destination, out long actualCopied) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, overwrite the first few elements in unmanaged pointer <paramref name="destination"/> by the <paramref name="values"/> of a managed array of type <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="destination">The destination <see cref="PointerSegment"/> to copy to</param>
		/// <param name="values">The managed <see cref="ReadOnlySpan{T}"/> of type <typeparamref name="T"/> to copy from</param>
		/// <param name="actualCopied">Output the number of elements (in <typeparamref name="T"/>) actually copied</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="destination"/> is invalid</exception>
		protected abstract bool FromManaged_<T>(PointerSegment destination, ReadOnlySpan<T> values, out long actualCopied) where T : unmanaged;

		// Ignore Spelling: sizeof
		/// <summary>
		/// When implemented by a derived class, copy out the elements in unmanaged pointer <paramref name="source"/> as a 2D matrix to a managed array of type <typeparamref name="T"/> (viewed as a 1D array).
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="source">The source <see cref="PointerSegment"/> to copy from</param>
		/// <param name="leadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="source"/></param>
		/// <param name="height">The height to copy in <typeparamref name="T"/></param>
		/// <param name="width">The width to copy in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="destination">The managed <see cref="Span{T}"/> of type <typeparamref name="T"/> to copy to, must has <c><see cref="Array.Length">Length</see> ¡Ý <paramref name="height"/> <paramref name="width"/></c></param>
		/// <param name="destinationLeadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="destination"/>, default 0 means <paramref name="height"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is invalid</exception>
		/// <exception cref="ArgumentException">
		/// If <paramref name="height"/> is larger than <paramref name="leadDim"/> or <paramref name="destinationLeadDim"/>,
		/// or <c><paramref name="leadDim"/> * <paramref name="width"/> * sizeof(<typeparamref name="T"/>) &gt; <paramref name="source"/>.<see cref="PointerSegment.LengthInBytes">Length</see></c>,
		/// or <c><paramref name="destinationLeadDim"/> * <paramref name="width"/> &gt; <paramref name="destination"/></c>.<see cref="Array.Length">Length</see>
		/// </exception>
		protected abstract bool ToManaged2D_<T>(PointerSegment source, long leadDim, long height, long width, Span<T> destination, long destinationLeadDim = 0) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, overwrite some of the elements in unmanaged pointer <paramref name="destination"/> as a 2D matrix by  a managed array of type <typeparamref name="T"/> (viewed as a 1D array).
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="destination">The destination <see cref="PointerSegment"/> to copy to</param>
		/// <param name="leadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="destination"/></param>
		/// <param name="height">The height to copy in <typeparamref name="T"/></param>
		/// <param name="width">The width to copy in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="values">The managed <see cref="ReadOnlySpan{T}"/> of type <typeparamref name="T"/> to copy from, must has <c><see cref="Array.Length">Length</see> ¡Ý <paramref name="height"/> * <paramref name="width"/></c></param>
		/// <param name="valuesLeadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="values"/>, default 0 means <paramref name="height"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="destination"/> is invalid</exception>
		/// <exception cref="ArgumentException">
		/// If <paramref name="height"/> is larger than <paramref name="leadDim"/> or <paramref name="valuesLeadDim"/>,
		/// or <c><paramref name="leadDim"/> * <paramref name="width"/> * sizeof(<typeparamref name="T"/>) &gt; <paramref name="destination"/>.<see cref="PointerSegment.LengthInBytes">Length</see></c>,
		/// or <c><paramref name="valuesLeadDim"/> * <paramref name="width"/> &gt; <paramref name="values"/></c>.<see cref="Array.Length">Length</see>
		/// </exception>
		protected abstract bool FromManaged2D_<T>(PointerSegment destination, long leadDim, long height, long width, ReadOnlySpan<T> values, long valuesLeadDim = 0) where T : unmanaged;
		#endregion

		#region high-level storage operations
		/// <summary>
		/// When implemented by a derived class, fill the <paramref name="storage"/> byte by byte to the same <paramref name="value"/>.<br/>
		/// The default implementation only works for <see cref="Storage{T}.LocationDescription"/>.<see cref="CombinationOfLocations.Type">Type</see> == <see cref="CombinationType.PureOrMixed"/> or <see cref="CombinationType.Cached"/>.<br/>
		/// The default implementation only uses <see cref="IsSupportedUnary(CombinationOfLocations)"/> to check the support; rewrite this method if that method doest not give correct support information of this one.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="storage">The <see cref="Storage{T}"/> to be filled</param>
		/// <param name="value">The value to fill</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="storage"/> is null or invalid</exception>
		protected virtual bool FillWithValue_<T>(Storage<T> storage, byte value) where T : unmanaged
		{
			storage.Cast(out IPureOrMixedStorage? mixed, out ICachedStorage? cached);
			if (!this.IsSupportedUnary(storage.LocationDescription))
				return false;
			if (mixed is not null)
			{
				for (int i = 0; i < storage.Count; i++)
				{
					this.FillWithValue_(storage[i], value);
				}
			}
			else if (cached is not null)
			{
				if (cached.LengthInBytes <= cached.GetRealLength() * ICachedStorage.CacheSizeRatio)
				{
					cached.Flush();
					this.FillWithValue_(cached[0], value);
				}
				else
				{
					cached.ApplyUnaryFunction(this.FillWithValue_, 0, cached.LengthInBytes, auxiliary: value, copyFunc: this.MemoryCopy_);
				}
			}
			return true;
		}

		/// <summary>
		/// When implemented by a derived class, fill the <paramref name="storage"/>'s each value by same <paramref name="value"/>.<br/>
		/// The default implementation only works for <see cref="Storage{T}.LocationDescription"/>.<see cref="CombinationOfLocations.Type">Type</see> == <see cref="CombinationType.PureOrMixed"/> or <see cref="CombinationType.Cached"/>.<br/>
		/// The default implementation only uses <see cref="IsSupportedUnary(CombinationOfLocations)"/> to check the support; rewrite this method if that method doest not give correct support information of this one.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="storage">The <see cref="Storage{T}"/> to be filled</param>
		/// <param name="value">The value to fill</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="storage"/> is null or invalid</exception>
		protected virtual bool FillWithValue_<T>(Storage<T> storage, T value) where T : unmanaged, IEquatable<T>
		{
			storage.Cast(out IPureOrMixedStorage? mixed, out ICachedStorage? cached);
			if (!this.IsSupportedUnary(storage.LocationDescription))
				return false;
			if (mixed is not null)
			{
				if (value.IsZero())
					for (int i = 0; i < storage.Count; i++)
						this.FillWithValue_(storage[i], value);
				else
					for (int i = 0; i < storage.Count; i++)
						this.FillWithValue_(storage[i], (byte)0);
			}
			else if (cached is not null)
			{
				if (cached.LengthInBytes <= cached.GetRealLength() * ICachedStorage.CacheSizeRatio)
				{
					cached.Flush();
					if (value.IsZero())
						this.FillWithValue_(cached[0], (byte)0);
					else
						this.FillWithValue_(cached[0], value);
				}
				else
				{
					if (value.IsZero())
						cached.ApplyUnaryFunction(this.FillWithValue_, 0, cached.LengthInBytes, auxiliary: (byte)0, copyFunc: this.MemoryCopy_);
					else
						cached.ApplyUnaryFunction(this.FillWithValue_, 0, cached.LengthInBytes, auxiliary: value, copyFunc: this.MemoryCopy_);
				}
			}
			return true;
		}

		/// <summary>
		/// When implemented by a derived class, copy memory from <paramref name="source"/> to <paramref name="destination"/>.<br/>
		/// The default implementation only works for <see cref="Storage{T}.LocationDescription"/>.<see cref="CombinationOfLocations.Type">Type</see> == <see cref="CombinationType.PureOrMixed"/> or <see cref="CombinationType.Cached"/>.<br/>
		/// The default implementation only uses <see cref="IsSupportedBinary(CombinationOfLocations, CombinationOfLocations)"/> to check the support; rewrite this method if that method doest not give correct support information of this one.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="source">The source <see cref="Storage{T}"/> to copy from</param>
		/// <param name="destination">The destination <see cref="Storage{T}"/> pointer to copy into</param>
		/// <param name="actualCopied">Output the number of elements (in <typeparamref name="T"/>) actually copied</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <remarks>The one with less length among <paramref name="source"/> and <paramref name="destination"/> is used as the actual copy length</remarks>
		/// <exception cref="NotSupportedException">If <paramref name="source"/> or <paramref name="destination"/> is not supported</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is null or invalid</exception>
		protected virtual bool MemoryCopy_<T>(Storage<T> source, Storage<T> destination, out long actualCopied) where T : unmanaged
		{
			actualCopied = 0;
			source.Cast(out IPureOrMixedStorage? srcMixed, out ICachedStorage? srcCached);
			destination.Cast(out IPureOrMixedStorage? dstMixed, out ICachedStorage? dstCached);
			if (!this.IsSupportedBinary(source.LocationDescription, destination.LocationDescription))
				return false;
			// normal cases
			if (srcMixed is not null && dstMixed is not null)
			{
				// shortcut
				if (source.Count == 1 && destination.Count == 1)
				{
					this.MemoryCopy_(source[0], destination[0], out _);
				}
				else
				{
					srcMixed.StorageMemoryCopy(dstMixed, this.MemoryCopy_);
				}
			}
			else if (srcMixed is not null && dstCached is not null)
			{
				srcMixed.StorageMemoryCopy(dstCached, this.MemoryCopy_);
			}
			else if (srcCached is not null && dstMixed is not null)
			{
				srcCached.StorageMemoryCopy(dstMixed, this.MemoryCopy_);
			}
			else if (srcCached is not null && dstCached is not null)
			{
				srcCached.StorageMemoryCopy(dstCached, this.MemoryCopy_);
			}
			actualCopied = Math.Min(source.Length, destination.Length);
			return true;
		}

		/// <summary>
		/// When implemented by a derived class, copy the <paramref name="source"/> storage to <paramref name="destination"/> storage with given strides.<br/>
		/// <c><paramref name="destination"/>[j] = <paramref name="source"/>[k] for i = 1,¡­,n; k = 1 + (i - 1)*<paramref name="incrementSource"/>, j = 1 + (i - 1)*<paramref name="incrementDestination"/></c>.<br/>
		/// The number of elements copied is calculated to the maximum possible value that does not exceeds the boundaries.<br/>
		/// The default implementation only works for <see cref="Storage{T}.LocationDescription"/>.<see cref="CombinationOfLocations.Type">Type</see> == <see cref="CombinationType.PureOrMixed"/> or <see cref="CombinationType.Cached"/>.<br/>
		/// The default implementation only uses <see cref="IsSupportedBinary(CombinationOfLocations, CombinationOfLocations)"/> to check the support; rewrite this method if that method doest not give correct support information of this one.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="source">The <see cref="Storage{T}"/> to copy from</param>
		/// <param name="incrementSource">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="source"/></param>
		/// <param name="destination">The <see cref="Storage{T}"/> to copy to</param>
		/// <param name="incrementDestination">The stride between consecutive elements (in <typeparamref name="T"/>) of <paramref name="destination"/></param>
		/// <param name="actualCopied">Output the number of elements (in <typeparamref name="T"/>) actually copied</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="incrementSource"/> or <paramref name="incrementDestination"/> is less than 1</exception>
		protected virtual bool StridedCopy_<T>(Storage<T> source, int incrementSource, Storage<T> destination, int incrementDestination, out long actualCopied) where T : unmanaged
		{
			actualCopied = 0;
			long srcLen = source.Length, dstLen = destination.Length;
			if (incrementSource <= 0 || incrementSource >= srcLen)
				throw new ArgumentOutOfRangeException(nameof(incrementSource));
			if (incrementDestination <= 0 || incrementDestination >= dstLen)
				throw new ArgumentOutOfRangeException(nameof(incrementDestination));

			bool CopyFunc(PointerSegment s, PointerSegment d, out long c)
			{
				return this.StridedCopy_<T>(s, incrementSource, d, incrementDestination, out c);
			}

			int srcAlign = Storage<T>.SizeOfT * incrementSource, dstAlign = Storage<T>.SizeOfT * incrementDestination;
			source.Cast(out IPureOrMixedStorage? srcMixed, out ICachedStorage? srcCached);
			destination.Cast(out IPureOrMixedStorage? dstMixed, out ICachedStorage? dstCached);
			if (!this.IsSupportedBinary(source.LocationDescription, destination.LocationDescription))
				return false;

			// normal cases
			if (srcMixed is not null && dstMixed is not null)
			{
				// shortcut
				if (source.Count == 1 && destination.Count == 1)
				{
					this.StridedCopy_<T>(source[0], incrementSource, destination[0], incrementDestination, out _);
				}
				else
				{
					srcMixed.StorageMemoryCopy(dstMixed, CopyFunc, sourceAlign: srcAlign, destinationAlign: dstAlign);
				}
			}
			else if (srcMixed is not null && dstCached is not null)
			{
				srcMixed.StorageMemoryCopy(dstCached, alignedCopy: CopyFunc, blockCopy: this.MemoryCopy_, sourceAlign: srcAlign, destinationAlign: dstAlign);
			}
			else if (srcCached is not null && dstMixed is not null)
			{
				srcCached.StorageMemoryCopy(dstMixed, alignedCopy: CopyFunc, blockCopy: this.MemoryCopy_, sourceAlign: srcAlign, destinationAlign: dstAlign);
			}
			else if (srcCached is not null && dstCached is not null)
			{
				srcCached.StorageMemoryCopy(dstCached, alignedCopy: CopyFunc, blockCopy: this.MemoryCopy_, sourceAlign: srcAlign, destinationAlign: dstAlign);
			}
			actualCopied = Math.Min((srcLen - 1) / incrementSource + 1, (dstLen - 1) / incrementDestination + 1);
			return true;
		}

		/// <summary>
		/// When implemented by a derived class, copy 2D data from <paramref name="source"/> to <paramref name="destination"/>.<br/>
		/// The default implementation only works for <see cref="Storage{T}.LocationDescription"/>.<see cref="CombinationOfLocations.Type">Type</see> == <see cref="CombinationType.PureOrMixed"/> or <see cref="CombinationType.Cached"/>.<br/>
		/// The default implementation only uses <see cref="IsSupportedBinary(CombinationOfLocations, CombinationOfLocations)"/> to check the support; rewrite this method if that method doest not give correct support information of this one.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="source">The source <see cref="Storage{T}"/></param>
		/// <param name="sourceLD">The source array actual height (actual leading dimension) in <typeparamref name="T"/></param>
		/// <param name="destination">The destination <see cref="Storage{T}"/></param>
		/// <param name="destLD">The destination array actual height (actual leading dimension) in <typeparamref name="T"/></param>
		/// <param name="height">The height to copy in <typeparamref name="T"/></param>
		/// <param name="width">The width to copy in the real type</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <remarks>The lengths of <paramref name="source"/> and <paramref name="destination"/> are <b>not</b> ignored</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If any of the parameters is zero</exception>
		/// <exception cref="ArgumentException">
		/// If <paramref name="height"/> is larger than <paramref name="sourceLD"/> or <paramref name="destLD"/>,
		/// or <c><paramref name="sourceLD"/> * <paramref name="width"/> &gt; <paramref name="source"/>.<see cref="Storage{T}.Length">Length</see></c>, 
		/// or <c><paramref name="destLD"/> * <paramref name="width"/> &gt; <paramref name="destination"/>.<see cref="Storage{T}.Length">Length</see></c>
		/// </exception>
		protected virtual bool MemoryCopy2D_<T>(Storage<T> source, long sourceLD, Storage<T> destination, long destLD, long height, long width) where T : unmanaged
		{
			if (sourceLD == 0)
				throw new ArgumentOutOfRangeException(nameof(sourceLD), Parameter.MustPositive);
			if (destLD == 0)
				throw new ArgumentOutOfRangeException(nameof(destLD), Parameter.MustPositive);
			if (width == 0)
				throw new ArgumentOutOfRangeException(nameof(width), Parameter.MustPositive);
			if (height == 0)
				throw new ArgumentOutOfRangeException(nameof(height), Parameter.MustPositive);
			if (height > sourceLD || height > destLD)
				throw new ArgumentException(Parameter.InvalidValue, nameof(height));
			if (sourceLD * width > source.LengthInBytes)
				throw new ArgumentException(Parameter.WrongSize, nameof(source));
			if (destLD * width > destination.LengthInBytes)
				throw new ArgumentException(Parameter.WrongSize, nameof(destination));

			source.Cast(out IPureOrMixedStorage? srcMixed, out ICachedStorage? _);
			destination.Cast(out IPureOrMixedStorage? dstMixed, out ICachedStorage? _);
			if (!this.IsSupportedBinary(source.LocationDescription, destination.LocationDescription))
				return false;

			// shortcut
			if (srcMixed is not null && dstMixed is not null && source.Count == 1 && destination.Count == 1)
			{
				return this.MemoryCopy2D_(source[0], sourceLD, destination[0], destLD, height, width);
			}
			long srcLD = sourceLD, dstLD = destLD;
			source = source.MakeReference(newLength: height);
			destination = destination.MakeReference(newLength: height);
			for (long column = 0; column < width - 1; column++)
			{
				this.MemoryCopy_(source, destination, out _);
				source = source.MakeReference(offset: srcLD, newLength: height);
				destination = destination.MakeReference(offset: dstLD, newLength: height);
			}
			// copy last column
			this.MemoryCopy_(source, destination, out _);
			return true;
		}
		#endregion

		#region high-level storage and managed operations
		/// <summary>
		/// When implemented by a derived class, copy out the <b>first</b> element in <see cref="Storage{T}"/> <paramref name="source"/> to a managed value of type <typeparamref name="T"/>.<br/>
		/// The default implementation only works for <see cref="Storage{T}.LocationDescription"/>.<see cref="CombinationOfLocations.Type">Type</see> == <see cref="CombinationType.PureOrMixed"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="source">The source <see cref="Storage{T}"/> to copy from</param>
		/// <param name="value">Output the first element in <paramref name="source"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		protected virtual bool ToManaged_<T>(Storage<T> source, out T value) where T : unmanaged
		{
			value = default;
			source.Cast(out IPureOrMixedStorage? mixed, out ICachedStorage? cached);

			if (mixed is not null)
			{
				return this.ToManaged_(mixed[0], out value);
			}
			else if (cached is not null)
			{
				var temp = cached.Retrieve(0, Storage<T>.SizeOfT);
				return this.ToManaged_(temp, out value);
			}
			return false;
		}

		/// <summary>
		/// When implemented by a derived class, overwrite the <b>first</b> element in unmanaged pointer <paramref name="destination"/> by a managed <paramref name="value"/> of type <typeparamref name="T"/>.<br/>
		/// The default implementation only works for <see cref="Storage{T}.LocationDescription"/>.<see cref="CombinationOfLocations.Type">Type</see> == <see cref="CombinationType.PureOrMixed"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="destination">The destination <see cref="Storage{T}"/> to copy to</param>
		/// <param name="value">The value of type <typeparamref name="T"/> to copy from</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="destination"/> is null or invalid</exception>
		protected virtual bool FromManaged_<T>(Storage<T> destination, T value) where T : unmanaged
		{
			destination.Cast(out IPureOrMixedStorage? mixed, out ICachedStorage? cached);

			if (mixed is not null)
			{
				return this.FromManaged_(mixed[0], value);
			}
			else if (cached is not null)
			{
				var temp = cached.Retrieve(0, Storage<T>.SizeOfT);
				return this.FromManaged_(temp, value);
			}
			return false;
		}

		/// <summary>
		/// When implemented by a derived class, copy out the first few elements in unmanaged pointer <paramref name="source"/> to a managed array of type <typeparamref name="T"/>.<br/>
		/// The default implementation only works for <see cref="Storage{T}.LocationDescription"/>.<see cref="CombinationOfLocations.Type">Type</see> == <see cref="CombinationType.PureOrMixed"/>.<br/>
		/// The default implementation only uses <see cref="CanTransferWithManaged(CombinationOfLocations)"/> to check the support; rewrite this method if that method doest not give correct support information of this one.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="source">The source <see cref="Storage{T}"/> to copy from</param>
		/// <param name="destination">The managed <see cref="Span{T}"/> of type <typeparamref name="T"/> to copy to</param>
		/// <param name="actualCopied">Output the number of elements (in <typeparamref name="T"/>) actually copied</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is null or invalid</exception>
		protected virtual bool ToManaged_<T>(Storage<T> source, Span<T> destination, out long actualCopied) where T : unmanaged
		{
			actualCopied = 0;
			source.Cast(out IPureOrMixedStorage? mixed, out ICachedStorage? cached);
			if (!this.CanTransferWithManaged(source.LocationDescription))
				return false;

			if (mixed is not null)
			{
				int offset = 0;
				for (int i = 0; i < mixed.Count; i++)
				{
					this.ToManaged_(mixed[i], destination[offset..], out _);
					if (offset >= destination.Length)
						break;
					offset += (int)(mixed[i].LengthInBytes / Storage<T>.SizeOfT);
				}
			}
			else if (cached is not null)
			{
				long count = Storage<T>.SizeOfT * (long)destination.Length;
				if (count >= cached.GetRealLength() / ICachedStorage.CacheSizeRatio)
				{
					cached.Flush();
					this.ToManaged_(cached[0], destination, out _);
				}
				else
				{
					int pack = Storage<T>.SizeOfT;
					long offset = 0;
					long maxCacheSize = cached.TopCacheSizeInBytes / pack * pack;
					while (count > 0)
					{
						long getLength = Math.Min(count, maxCacheSize);
						var temp = cached.Retrieve(offset, count, this.MemoryCopy_);
						Span<T> val = destination.Slice((int)(offset / pack), (int)(getLength / pack));
						this.ToManaged_(temp, val, out _);
						offset += getLength;
						count -= getLength;
					}
				}
			}
			actualCopied = Math.Min(source.Length, destination.Length);
			return true;
		}

		/// <summary>
		/// When implemented by a derived class, overwrite the first few elements in unmanaged pointer <paramref name="destination"/> by the <paramref name="values"/> of a managed array of type <typeparamref name="T"/>.<br/>
		/// The default implementation only works for <see cref="Storage{T}.LocationDescription"/>.<see cref="CombinationOfLocations.Type">Type</see> == <see cref="CombinationType.PureOrMixed"/>.<br/>
		/// The default implementation only uses <see cref="CanTransferWithManaged(CombinationOfLocations)"/> to check the support; rewrite this method if that method doest not give correct support information of this one.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="destination">The destination <see cref="Storage{T}"/> to copy to</param>
		/// <param name="values">The managed <see cref="ReadOnlySpan{T}"/> of type <typeparamref name="T"/> to copy from</param>
		/// <param name="actualCopied">Output the number of elements (in <typeparamref name="T"/>) actually copied</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="destination"/> is invalid</exception>
		protected virtual bool FromManaged_<T>(Storage<T> destination, ReadOnlySpan<T> values, out long actualCopied) where T : unmanaged
		{
			actualCopied = 0;
			destination.Cast(out IPureOrMixedStorage? mixed, out ICachedStorage? cached);
			if (!this.CanTransferWithManaged(destination.LocationDescription))
				return false;

			if (mixed is not null)
			{
				int offset = 0;
				for (int i = 0; i < destination.Count; i++)
				{
					this.FromManaged_(destination[i], values[offset..], out _);
					if (offset >= values.Length)
						break;
					offset += (int)(destination[i].LengthInBytes / Storage<T>.SizeOfT);
				}
			}
			else if (cached is not null)
			{
				long count = Storage<T>.SizeOfT * (long)values.Length;
				if (count >= cached.GetRealLength() / ICachedStorage.CacheSizeRatio)
				{
					cached.Flush();
					this.FromManaged_(cached[0], values, out _);
				}
				else
				{
					int pack = Storage<T>.SizeOfT;
					long offset = 0;
					long maxCacheSize = cached.TopCacheSizeInBytes / pack * pack;
					while (count > 0)
					{
						long getLength = Math.Min(count, maxCacheSize);
						var temp = cached.Retrieve(offset, count, this.MemoryCopy_);
						ReadOnlySpan<T> val = values.Slice((int)(offset / pack), (int)(getLength / pack));
						this.FromManaged_(temp, val, out _);
						offset += getLength;
						count -= getLength;
					}
				}
			}
			actualCopied = Math.Min(destination.Length, values.Length);
			return true;
		}

		/// <summary>
		/// When implemented by a derived class, copy out the elements in unmanaged pointer <paramref name="source"/> as a 2D matrix to a managed array of type <typeparamref name="T"/> (viewed as a 1D array).<br/>
		/// The default implementation only works for <see cref="Storage{T}.LocationDescription"/>.<see cref="CombinationOfLocations.Type">Type</see> == <see cref="CombinationType.PureOrMixed"/>.<br/>
		/// The default implementation only uses <see cref="CanTransferWithManaged(CombinationOfLocations)"/> to check the support; rewrite this method if that method doest not give correct support information of this one.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="source">The source <see cref="Storage{T}"/> to copy from</param>
		/// <param name="leadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="source"/></param>
		/// <param name="height">The height to copy in <typeparamref name="T"/></param>
		/// <param name="width">The width to copy in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="destination">The managed <see cref="Span{T}"/> of type <typeparamref name="T"/> to copy to, must has <c><see cref="Array.Length">Length</see> ¡Ý <paramref name="height"/> * <paramref name="width"/></c></param>
		/// <param name="destinationLeadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="destination"/>, default 0 means <paramref name="height"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> is invalid</exception>
		/// <exception cref="ArgumentException">
		/// If <paramref name="height"/> is larger than <paramref name="leadDim"/> or <paramref name="destinationLeadDim"/>,
		/// or <c><paramref name="leadDim"/> * <paramref name="width"/> &gt; <paramref name="source"/>.<see cref="Storage{T}.Length">Length</see></c>,
		/// or <c><paramref name="destinationLeadDim"/> * <paramref name="width"/> &gt; <paramref name="destination"/></c>.<see cref="Array.Length">Length</see>
		/// </exception>
		protected virtual bool ToManaged2D_<T>(Storage<T> source, long leadDim, long height, long width, Span<T> destination, long destinationLeadDim = 0) where T : unmanaged
		{
			if (!source.IsValid())
				throw new ArgumentNullException(nameof(source));
			if (leadDim == 0)
				throw new ArgumentOutOfRangeException(nameof(leadDim), Parameter.MustPositive);
			if (width == 0)
				throw new ArgumentOutOfRangeException(nameof(width), Parameter.MustPositive);
			if (height == 0)
				throw new ArgumentOutOfRangeException(nameof(height), Parameter.MustPositive);
			if (height > leadDim || height > destinationLeadDim)
				throw new ArgumentException(Parameter.InvalidValue, nameof(height));
			if (leadDim * width > source.Length)
				throw new ArgumentException(Parameter.WrongSize, nameof(source));
			if (leadDim * width > destination.Length)
				throw new ArgumentException(Parameter.WrongSize, nameof(destination));
			// shortcut
			if (source.Count == 1)
			{
				return this.ToManaged2D_(source[0], leadDim, height, width, destination, destinationLeadDim);
			}
			// normal case
			if (!this.CanTransferWithManaged(source.LocationDescription))
				return false;

			int h = checked((int)height), dstLD = checked((int)(destinationLeadDim == 0 ? height : destinationLeadDim));
			long srcLD = leadDim, max = leadDim * width;
			int dstOffset = 0;
			for (long srcOffset = 0; srcOffset < max; srcOffset += srcLD, dstOffset += dstLD)
			{
				var src = source.MakeReference(offset: srcOffset, newLength: height);
				var dst = destination.Slice(dstOffset, h);
				this.ToManaged_(src, dst, out _);
			}
			return true;
		}

		/// <summary>
		/// When implemented by a derived class, overwrite some of the elements in unmanaged pointer <paramref name="destination"/> as a 2D matrix by  a managed array of type <typeparamref name="T"/> (viewed as a 1D array).<br/>
		/// The default implementation only works for <see cref="Storage{T}.LocationDescription"/>.<see cref="CombinationOfLocations.Type">Type</see> == <see cref="CombinationType.PureOrMixed"/>.<br/>
		/// The default implementation only uses <see cref="CanTransferWithManaged(CombinationOfLocations)"/> to check the support; rewrite this method if that method doest not give correct support information of this one.
		/// </summary>
		/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
		/// <param name="destination">The destination <see cref="Storage{T}"/> to copy to</param>
		/// <param name="leadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="destination"/></param>
		/// <param name="height">The height to copy in <typeparamref name="T"/></param>
		/// <param name="width">The width to copy in <typeparamref name="T"/> rather than bytes</param>
		/// <param name="values">The managed <see cref="ReadOnlySpan{T}"/> of type <typeparamref name="T"/> to copy from, must has <c><see cref="Array.Length">Length</see> ¡Ý <paramref name="height"/> * <paramref name="width"/></c></param>
		/// <param name="valuesLeadDim">The actual height (actual leading dimension) in <typeparamref name="T"/> of <paramref name="values"/>, default 0 means <paramref name="height"/></param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="destination"/> is invalid</exception>
		/// <exception cref="ArgumentException">
		/// If <paramref name="height"/> is larger than <paramref name="leadDim"/> or <paramref name="valuesLeadDim"/>,
		/// or <c><paramref name="leadDim"/> * <paramref name="width"/> &gt; <paramref name="destination"/>.<see cref="Storage{T}.Length">Length</see></c>,
		/// or <c><paramref name="valuesLeadDim"/> * <paramref name="width"/> &gt; <paramref name="values"/></c>.<see cref="Array.Length">Length</see>
		/// </exception>
		protected virtual bool FromManaged2D_<T>(Storage<T> destination, long leadDim, long height, long width, ReadOnlySpan<T> values, long valuesLeadDim = 0) where T : unmanaged
		{
			if (!destination.IsValid())
				throw new ArgumentNullException(nameof(destination));
			if (leadDim == 0)
				throw new ArgumentOutOfRangeException(nameof(leadDim), Parameter.MustPositive);
			if (width == 0)
				throw new ArgumentOutOfRangeException(nameof(width), Parameter.MustPositive);
			if (height == 0)
				throw new ArgumentOutOfRangeException(nameof(height), Parameter.MustPositive);
			if (height > leadDim || height > valuesLeadDim)
				throw new ArgumentException(Parameter.InvalidValue, nameof(height));
			if (leadDim * width > destination.Length)
				throw new ArgumentException(Parameter.WrongSize, nameof(destination));
			if (leadDim * width > values.Length)
				throw new ArgumentException(Parameter.WrongSize, nameof(values));
			// shortcut
			if (destination.Count == 1)
			{
				return this.FromManaged2D_(destination[0], leadDim, height, width, values);
			}
			// normal case
			if (!this.CanTransferWithManaged(destination.LocationDescription))
				return false;

			int h = checked((int)height), srcLD = checked((int)(valuesLeadDim == 0 ? height : valuesLeadDim));
			long dstLD = leadDim, max = leadDim * width;
			int srcOffset = 0;
			for (long dstOffset = 0; dstOffset < max; dstOffset += dstLD, srcOffset += srcLD)
			{
				var dst = destination.MakeReference(offset: dstOffset, newLength: height);
				var src = values.Slice(srcOffset, h);
				this.FromManaged_(dst, src, out _);
			}
			return true;
		}
		#endregion
		#endregion
	}
}