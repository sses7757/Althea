using System;
using System.Runtime.CompilerServices;

using Althea.NativeTypes;
using Althea.Resources;


namespace Althea.Storage
{
	/// <summary>
	/// The base interface for all storage classes
	/// </summary>
	public interface IStorage : ICheckValid, IDisposable
	{
		/// <summary>
		/// Whether this storage is disposed or not
		/// </summary>
		protected bool Disposed { get; }

		void IDisposable.Dispose()
		{
			if (this.IsValid())
				this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// When implemented by a derived class, get the total length of the presenting array in bytes
		/// </summary>
		long LengthInBytes { get; }

		/// <summary>
		/// When implemented by a derived class, actually unmanaged resources held by this <see cref="IStorage"/>
		/// </summary>
		/// <param name="invokedByUser">Whether this method is invoked by user or by GC</param>
		protected abstract void Dispose(bool invokedByUser);

		/// <summary>
		/// When implemented by a derived class, statically get the data type of this storage as a <see cref="NativeTypes.DataType"/>
		/// </summary>
		abstract static DataType DataType { get; }

		/// <summary>
		/// When implemented by a derived class, statically get the description of the storage locations of this <see cref="IStorage"/> as a <see cref="CombinationOfLocations"/>
		/// </summary>
		abstract static CombinationOfLocations LocationDescription { get; }
	}

	/// <summary>
	/// The interface for wrapper of unmanaged memory block(s) of different <see cref="StorageLocation"/>(s) of type <typeparamref name="T"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TSelf">The actual class that implement <see cref="IStorage{T, TSelf}"/></typeparam>
	public partial interface IStorage<T, TSelf> : IStorage,
		IEqualityOperators<TSelf, TSelf>, ICloneable<TSelf>, IMainPropertyFormattable<TSelf>,
		IAdditiveIdentity<TSelf, long>, IAdditionOperators<TSelf, long, TSelf>, ISubtractionOperators<TSelf, long, TSelf>
		where T : unmanaged, INumber<T>
		where TSelf : class, IStorage<T, TSelf>
	{
		/// <summary>
		/// When implemented by a derived class, check whether this <typeparamref name="TSelf"/> is valid or not after moving an <paramref name="offset"/> and set <see cref="Length"/> to <paramref name="newLength"/>
		/// </summary>
		/// <param name="offset">The offset to move in <typeparamref name="T"/></param>
		/// <param name="newLength">The length to check in <typeparamref name="T"/>, default 0 means auto calculation by <paramref name="offset"/></param>
		/// <returns>The validness of this <typeparamref name="TSelf"/> under <paramref name="offset"/> and <paramref name="newLength"/></returns>
		/// <remarks>Default implementation utilizes <see cref="Length"/> and <see cref="IReferenceStorage{T, TStorage}.TotalOffset"/></remarks>
		public virtual bool IsOffsetValid(long offset, long newLength = 0)
		{
			if (newLength < 0 || !this.IsValid())
				return false;
			if (this is IReferenceStorage<T, TSelf> reference)
			{
				if (reference.Reference is null)
					return false;
				offset += reference.TotalOffset;
				if (offset < 0 || offset >= reference.Reference.LengthInBytes / Unmanaged<T>.Size)
					return false;
				if (newLength > 0 && newLength + offset > reference.Reference.LengthInBytes / Unmanaged<T>.Size)
					return false;
				return true;
			}
			else
			{
				if (offset < 0 || offset >= this.Length)
					return false;
				if (newLength > 0 && newLength + offset > this.Length)
					return false;
				return true;
			}
		}

		/// <summary>
		/// Request usage of a piece of storage started from <paramref name="offset"/> with <paramref name="length"/> and will be used as <paramref name="intentWrite"/>.
		/// </summary>
		/// <param name="offset">The starting requesting element offset compared to this storage</param>
		/// <param name="length">The number of element(s) requested</param>
		/// <param name="intentWrite">The usage intent is to write (true) or to read (false)</param>
		/// <returns>The maximum length from <paramref name="offset"/> allowed for request, or 0 if <paramref name="length"/> is allowed.</returns>
		/// <remarks>The default implementation simply returns 0 for performance issues, invoke <see cref="IsOffsetValid(long, long)"/> to check parameters if necessary.</remarks>
		public virtual long Request(long offset, long length, bool intentWrite) => 0;

		/// <summary>
		/// When implemented by a derived class, check whether this <typeparamref name="TSelf"/> has same origin as the <paramref name="other"/> <typeparamref name="TSelf"/>.
		/// </summary>
		/// <param name="other">The other <typeparamref name="TSelf"/> to check overlap</param>
		/// <returns>True if this storage has same origin with the <paramref name="other"/>, false otherwise</returns>
		/// <exception cref="NotImplementedException">If this <typeparamref name="TSelf"/> or the <paramref name="other"/> <typeparamref name="TSelf"/> is neither an <see cref="IActualStorage{T, TStorage}"/> nor an <see cref="IReferenceStorage{T, TStorage}"/>.</exception>
		public virtual bool SameOriginAs(TSelf other)
		{
			if (!this.IsValid() || !other.IsValid())
				return false;

			var originThis = this as IActualStorage<T, TSelf> ?? (this as IReferenceStorage<T, TSelf>)?.Reference;
			var originOther = other as IActualStorage<T, TSelf> ?? (other as IReferenceStorage<T, TSelf>)?.Reference;
			if (originThis is null || originOther is null)
				throw new NotImplementedException();
			return originThis.Equals(originOther);
		}

		/// <summary>
		/// When implemented by a derived class, check whether this <typeparamref name="TSelf"/> overlaps with the <paramref name="other"/> <typeparamref name="TSelf"/>.
		/// </summary>
		/// <param name="other">The other <typeparamref name="TSelf"/> to check overlap</param>
		/// <returns>True if this overlaps with the <paramref name="other"/>, false otherwise</returns>
		bool OverlapWith(TSelf other);

		TSelf ICloneable<TSelf>.Clone()
		{
			var storage = TSelf.CreateAlike<T, TSelf>((TSelf)this);
			try
			{
				this.CopyTo<T, TSelf>(storage);
				return storage;
			}
			catch (System.Exception)
			{
				storage?.Dispose();
				throw;
			}
		}

		/// <summary>
		/// When implemented by a derived class, statically get an empty <typeparamref name="TSelf"/>
		/// </summary>
		abstract static TSelf Empty { get; }

		/// <summary>
		/// When implemented by a derived class, statically create a referenced storage of type <typeparamref name="TSelf"/> over <paramref name="storage"/> of data type <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged number as the new data type</typeparam>
		/// <typeparam name="TOther">Any storage type that implements <see cref="IStorage{TOut, TOther}"/></typeparam>
		/// <param name="storage">The storage of data type <typeparamref name="TOut"/> to mimic.</param>
		/// <returns>A referenced storage of type <typeparamref name="TSelf"/> over <paramref name="storage"/></returns>
		/// <exception cref="InvalidCastException">If a referenced <typeparamref name="TOther"/> cannot be created from <typeparamref name="TSelf"/></exception>
		abstract static TSelf RefFrom<TOut, TOther>(TOther storage) where TOut : unmanaged, INumber<TOut> where TOther : class, IStorage<TOut, TOther>;

		/// <summary>
		/// When implemented by a derived class, statically allocate and creates a new <typeparamref name="TSelf"/> alike <paramref name="storage"/>.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged number as the new data type</typeparam>
		/// <typeparam name="TOther">Any storage type that implements <see cref="IStorage{TOut, TOther}"/></typeparam>
		/// <param name="storage">The storage of data type <typeparamref name="TOut"/> to mimic.</param>
		/// <returns>A new <typeparamref name="TSelf"/> that likes <paramref name="storage"/></returns>
		/// <exception cref="InvalidCastException">If an actual storage <typeparamref name="TOther"/> cannot be created alike <typeparamref name="TSelf"/></exception>
		abstract static TSelf CreateAlike<TOut, TOther>(TOther storage) where TOut : unmanaged, INumber<TOut> where TOther : class, IStorage<TOut, TOther>;

		/// <summary>
		/// Statically get the distance in bytes between two <typeparamref name="TSelf"/>s
		/// </summary>
		/// <param name="left">The left operand of type <typeparamref name="TSelf"/></param>
		/// <param name="right">The right operand of type <typeparamref name="TSelf"/></param>
		/// <returns>The distance between two <typeparamref name="TSelf"/>s in bytes as a <see cref="long"/>.</returns>
		/// <exception cref="InvalidOperationException">If <paramref name="left"/> and <paramref name="right"/> have different origin.</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static long StorageDiffBytes(TSelf left, TSelf right)
		{
			if (!left.IsValid())
				throw new ArgumentNullException(nameof(left));
			if (!right.IsValid())
				throw new ArgumentNullException(nameof(right));
			// check same origin
			if (!left.SameOriginAs(right))
				throw new InvalidOperationException();
			IActualStorage<T, TSelf>? actualLeft = left as IActualStorage<T, TSelf>, actualRight = right as IActualStorage<T, TSelf>;
			IReferenceStorage<T, TSelf>? refLeft = left as IReferenceStorage<T, TSelf>, refRight = right as IReferenceStorage<T, TSelf>;
			// check offset divisible
			if (actualLeft is not null && refRight is not null)
				return -refRight.TotalOffsetInBytes;
			else if (refLeft is not null && actualRight is not null)
				return refLeft.TotalOffsetInBytes;
			else if (refLeft is not null && refRight is not null)
				return refRight.TotalOffsetInBytes - refLeft.TotalOffsetInBytes;
			else
				return 0;
		}

		/// <summary>
		/// Get the total length of the presenting array in type <typeparamref name="T"/>. The default implementation uses <see cref="Unmanaged{T}.Size"/>.
		/// </summary>
		public virtual long Length => LengthInBytes / Unmanaged<T>.Size;

		/// <summary>
		/// When implemented by a derived class, make a referenced <typeparamref name="TSelf"/> with the starting pointer moving <paramref name="offset"/> and <see cref="Length"/> changing to <paramref name="newLength"/>.
		/// </summary>
		/// <param name="offset">The offset in <typeparamref name="T"/> to the starting pointer of this <typeparamref name="TSelf"/> as a <see cref="long"/></param>
		/// <param name="newLength">The new length in <typeparamref name="T"/> as a <see cref="long"/>, default 0 means automatically calculate from <paramref name="offset"/></param>
		/// <returns>A referenced <typeparamref name="TSelf"/> of this one</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> and <paramref name="newLength"/> is out of boundary</exception>
		TSelf MakeReference(long offset = 0, long newLength = 0);

		/// <summary>
		/// When implemented by a derived class, statically get the distance in <typeparamref name="T"/> between two <typeparamref name="TSelf"/>s
		/// </summary>
		/// <param name="left">The left operand of type <typeparamref name="TSelf"/></param>
		/// <param name="right">The right operand of type <typeparamref name="TSelf"/></param>
		/// <returns>The distance between two <typeparamref name="TSelf"/>s in <typeparamref name="T"/> as a <see cref="long"/>.</returns>
		/// <exception cref="InvalidOperationException">If <paramref name="left"/> and <paramref name="right"/> have different origin.</exception>
		abstract static long operator -(TSelf left, TSelf right);

		/// <summary>
		/// When implemented by a derived class, statically <b>allocate</b> and create a new <typeparamref name="TSelf"/> of given lengths on different locations in <see cref="IStorage.LocationDescription"/>.
		/// </summary>
		/// <param name="lengths">The given lengths in <typeparamref name="T"/></param>
		/// <returns>The created new <typeparamref name="TSelf"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="lengths"/> is null or empty</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="lengths"/> has length(s) ≤ 0</exception>
		/// <exception cref="OutOfMemoryException">If the underlying allocation failed due to insufficient memory</exception>
		/// <exception cref="InvalidOperationException">If underlying creation fails due to other reasons</exception>
		abstract static TSelf Create(ReadOnlySpan<long> lengths);

		/// <summary>
		/// Check whether the given <paramref name="size"/> in <typeparamref name="T"/> can be casted without loss to <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">The output data type</typeparam>
		/// <param name="size">The size to check</param>
		/// <param name="sizeInBytes">Whether <paramref name="size"/> is in bytes or in <typeparamref name="T"/></param>
		/// <returns>The <paramref name="size"/> (multiplies the size of <typeparamref name="T"/> then) divides the size of <typeparamref name="TOut"/></returns>
		/// <exception cref="InvalidCastException">if <paramref name="size"/>( multiplies the size of <typeparamref name="T"/>) cannot be divided by the size of <typeparamref name="TOut"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static long CheckCast<TOut>(long size, bool sizeInBytes = false) where TOut : unmanaged, INumber<TOut>
		{
			long newSize = sizeInBytes ? size : (size * Unmanaged<T>.Size);
			if (size * Unmanaged<T>.Size % Unmanaged<TOut>.Size != 0)
				throw new InvalidCastException(Other.CannotDivide);
			newSize /= Unmanaged<TOut>.Size;
			return newSize;
		}
	}

	/// <summary>
	/// The interface for an actual storage of data type <typeparamref name="T"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TStorage">The actual class that implement <see cref="IStorage{T, TSelf}"/></typeparam>
	public interface IActualStorage<T, TStorage> : IStorage<T, TStorage> where T : unmanaged, INumber<T> where TStorage : class, IStorage<T, TStorage>
	{
	}

	/// <summary>
	/// The interface for a referenced storage of data type <typeparamref name="T"/> and storage type <typeparamref name="TStorage"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TStorage">The actual class that implement <see cref="IStorage{T, TStorage}"/></typeparam>
	public interface IReferenceStorage<T, TStorage> : IStorage<T, TStorage> where T : unmanaged, INumber<T> where TStorage : class, IStorage<T, TStorage>
	{
		void IStorage.Dispose(bool invokedByUser) { }

		/// <summary>
		/// When implemented by a derived class, get the referenced storage as a nullable <see cref="IStorage"/>
		/// </summary>
		IStorage? Reference { get; }

		/// <summary>
		/// When implemented by a derived class, get the total offset compared to the start of <see cref="Reference"/> in bytes
		/// </summary>
		long TotalOffsetInBytes { get; }

		/// <summary>
		/// Create a referenced <typeparamref name="TStorage"/> with given reference <paramref name="storage"/> and <paramref name="offset"/> to it
		/// </summary>
		/// <typeparam name="TS">The type constrain for <paramref name="storage"/></typeparam>
		/// <param name="storage">The <see cref="IStorage"/> to be referenced</param>
		/// <param name="offset">The total offset compared to <paramref name="storage"/> as a <see cref="long"/></param>
		/// <param name="newLength">The new presenting length. A value less than or equals to 0 means the maximum possible value calculate from <paramref name="storage"/> and <paramref name="offset"/></param>
		/// <param name="sizeInBytes">Whether <paramref name="offset"/> and <paramref name="newLength"/> are in bytes or in <typeparamref name="T"/></param>
		/// <returns>The reference as a nullable <see cref="IStorage"/> and the real total offset and length in bytes</returns>
		/// <exception cref="ArgumentException">If <paramref name="storage"/> is not a <typeparamref name="TS"/></exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> and <paramref name="newLength"/> is out of boundary</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static (IStorage? reference, long totalOffsetBytes, long lengthBytes) Create<TS>(IStorage? storage, long offset = 0, long newLength = 0, bool sizeInBytes = false)
		{
			if (storage is null)
				return default;
			if (!sizeInBytes)
			{
				offset *= Unmanaged<T>.Size;
				newLength *= Unmanaged<T>.Size;
			}
			// get offset and new length in bytes
			if (newLength <= 0)
				newLength = storage.LengthInBytes - offset;
			// dereference first
			while (storage is IReferenceStorage<T, TStorage> @ref)
			{
				if (@ref.Reference is null)
					return default;
				storage = @ref.Reference;
				offset += @ref.TotalOffsetInBytes;
			}
			// check
			if (storage is not TS)
				throw new ArgumentException(Parameter.UnexpectedType, nameof(storage));
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, Parameter.CannotNegative);
			if (storage.LengthInBytes < offset + newLength)
				throw new ArgumentOutOfRangeException(nameof(newLength), newLength, Parameter.InvalidValue);
			// return
			return (storage, offset, newLength);
		}

		/// <summary>
		/// Get the total offset compared to the start of the underlying reference in <typeparamref name="T"/>.
		/// </summary>
		/// <remarks>The default implementation does not check whether <see cref="TotalOffsetInBytes"/> can be divided by <see cref="Unmanaged{T}.Size"/> or not.</remarks>
		public virtual long TotalOffset => TotalOffsetInBytes / Unmanaged<T>.Size;
	}
}
