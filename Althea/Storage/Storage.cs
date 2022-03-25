using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

using Althea.Helpers;
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
		/// When implemented by a derived class, get the referenced storage of this storage as a nullable <see cref="IStorage"/>.
		/// </summary>
		IStorage? Reference => null;

		/// <summary>
		/// When implemented by a derived class, get the total offset compared to the start of <see cref="Reference"/> in bytes
		/// </summary>
		long TotalOffsetInBytes => 0;

		/// <summary>
		/// Whether this storage is disposed or not.
		/// </summary>
		bool Disposed { get; }

		void IDisposable.Dispose()
		{
			if (this.IsValid())
				this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// When implemented by a derived class, actually unmanaged resources held by this <see cref="IStorage"/>.
		/// </summary>
		/// <param name="invokedByUser">Whether this method is invoked by user or by GC</param>
		public void Dispose(bool invokedByUser);

		/// <summary>
		/// When implemented by a derived class, get the total length of the presenting array in bytes.
		/// </summary>
		long LengthInBytes { get; }

		/// <summary>
		/// When implemented by a derived class, statically get the data type of this storage as a <see cref="NativeTypes.DataType"/>.
		/// </summary>
		abstract static DataType DataType { get; }

		/// <summary>
		/// When implemented by a derived class, statically get the description of the storage locations of this <see cref="IStorage"/> as a <see cref="CombinationOfLocations"/>.
		/// </summary>
		abstract static CombinationOfLocations LocationDescription { get; }

		/// <summary>
		/// When implemented by a derived class, statically get the <see cref="MethodInfo"/> of the pointers' getter methods in defined order.
		/// </summary>
		internal protected abstract static MethodInfo[] PointerGetters { get; }

		/// <summary>
		/// When implemented by a derived class, get the size of the pointer getter method of index <paramref name="i"/> defined in <see cref="PointerGetters"/>. The default implementation simply returns 1.
		/// </summary>
		/// <param name="i">The index of the pointer getter method</param>
		/// <returns>The size of the pointer getter method of index <paramref name="i"/>.</returns>
		/// <remarks>If the implemented class returns values larger than 1 for some pointers, it shall implement pointers' getters like <c>public <see cref="PointerSegment{T}"/> PointerN(<see cref="long"/> index, <see cref="bool"/> intentWrite) { ... }</c></remarks>
		internal protected virtual long SizeOfPointer(int i) => 1;

		/// <summary>
		/// When implemented by a derived class, check whether this storage is valid or not after moving <paramref name="offset"/> bytes and set length to <paramref name="newLength"/> bytes.
		/// </summary>
		/// <param name="offset">The offset to move in bytes, can be negative</param>
		/// <param name="newLength">The length to check in bytes, default 0 means auto calculation by <paramref name="offset"/></param>
		/// <returns>The validness of this storage under <paramref name="offset"/> and <paramref name="newLength"/>.</returns>
		bool IsByteOffsetValid(long offset, long newLength = 0);

		/// <summary>
		/// When implemented by a derived class, check whether this storage overlaps with the <paramref name="other"/> storage.
		/// </summary>
		/// <param name="other">The other <see cref="IStorage"/> to check overlap</param>
		/// <returns>True if this overlaps with the <paramref name="other"/>, false otherwise</returns>
		bool OverlapWith(IStorage other);
	}

	/// <summary>
	/// The interface for wrapper of unmanaged memory block(s) of different <see cref="StorageLocation"/>(s) of type <typeparamref name="T"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TSelf">The actual class that implement <see cref="IStorage{T, TSelf}"/></typeparam>
	public interface IStorage<T, TSelf> : IStorage, IReadOnlyList<T>,
		 ICreateAlike<TSelf>, IEqualityOperators<TSelf, TSelf>, IMainPropertyFormattable<TSelf>,
		IAdditiveIdentity<TSelf, long>, IAdditionOperators<TSelf, long, TSelf>, ISubtractionOperators<TSelf, long, TSelf>
		where T : unmanaged, INumber<T>
		where TSelf : class, IStorage<T, TSelf>
	{
		T IReadOnlyList<T>.this[int index] => ((TSelf)this.MakeReference(index, 1)).ToManaged<T, TSelf>();

		int IReadOnlyCollection<T>.Count => checked((int)this.Length);

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => this.GetEnumerator();

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			long length = this.Length;
			Span<T> buffer = stackalloc T[(int)Math.Min(length, Settings.StackAllocLimit / Unmanaged<T>.Size)];
			long offset = 0;
			while (offset < length)
			{
				((TSelf)this + offset).ToManaged(buffer);
				for (int i = 0; i < buffer.Length; i++)
				{
					yield return buffer[i];
				}
				offset += buffer.Length;
			}
		}

		/// <summary>
		/// Provide C# duck type method for range slicing of this storage.
		/// </summary>
		TSelf Slice(int start, int count) => this.MakeReference(start, count);

		bool IStorage.IsByteOffsetValid(long offset, long newLength)
		{
			if (newLength < 0 || !this.IsValid())
				return false;
			if (this is IReferenceStorage<T, TSelf> reference)
			{
				if (reference.Reference is null)
					return false;
				offset += reference.TotalOffsetInBytes;
				if (offset < 0 || offset >= reference.Reference.LengthInBytes)
					return false;
				if (newLength > 0 && newLength + offset > reference.Reference.LengthInBytes)
					return false;
				return true;
			}
			else
			{
				if (offset < 0 || offset >= this.LengthInBytes)
					return false;
				if (newLength > 0 && newLength + offset > this.LengthInBytes)
					return false;
				return true;
			}
		}

		/// <summary>
		/// Check whether this storage is valid or not after moving <paramref name="offset"/> elements and set length to <paramref name="newLength"/>.
		/// </summary>
		/// <param name="offset">The offset to move in <typeparamref name="T"/>, can be negative</param>
		/// <param name="newLength">The length to check in <typeparamref name="T"/>, default 0 means auto calculation by <paramref name="offset"/></param>
		/// <returns>The validness of this storage under <paramref name="offset"/> and <paramref name="newLength"/>.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public virtual bool IsOffsetValid(long offset, long newLength = 0) => this.IsByteOffsetValid(offset * Unmanaged<T>.Size, newLength * Unmanaged<T>.Size);

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

		TSelf ICloneable<TSelf>.Clone()
		{
			var storage = TSelf.CreateAlike<T, TSelf>((TSelf)this);
			try
			{
				((TSelf)this).CopyTo<T, TSelf, TSelf>(storage);
				return storage;
			}
			catch (Exception)
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
		/// <exception cref="OutOfMemoryException">If the underlying allocation failed due to insufficient memory</exception>
		/// <exception cref="InvalidCastException">If an actual storage <typeparamref name="TOther"/> cannot be created alike <typeparamref name="TSelf"/></exception>
		abstract static TSelf CreateAlike<TOut, TOther>(TOther storage) where TOut : unmanaged, INumber<TOut> where TOther : class, IStorage<TOut, TOther>;

		/// <summary>
		/// When implemented by a derived class, statically create a new <typeparamref name="TSelf"/> of given lengths.
		/// </summary>
		/// <param name="lengths">The given lengths in <typeparamref name="T"/></param>
		/// <returns>The created new <typeparamref name="TSelf"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="lengths"/> is null or empty</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="lengths"/> has length(s) ≤ 0</exception>
		/// <exception cref="OutOfMemoryException">If the underlying allocation failed due to insufficient memory</exception>
		/// <exception cref="InvalidOperationException">If underlying creation fails due to other reasons</exception>
		abstract static TSelf Create(ReadOnlySpan<long> lengths);

		TSelf ICreateAlike<TSelf>.CreateAlike() => TSelf.CreateAlike<T, TSelf>((TSelf)this);

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
		public long Length => this.LengthInBytes / Unmanaged<T>.Size;

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
				throw new InvalidCastException(ArithmeticError.CannotDivide);
			newSize /= Unmanaged<TOut>.Size;
			return newSize;
		}

		/// <summary>
		/// When implemented by a derived class, statically get the JSON converter of <typeparamref name="TSelf"/>. Cannot be null.
		/// </summary>
		protected internal abstract static JsonConverter<TSelf> JsonConverter { get; }
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
		abstract IStorage? IStorage.Reference { get; }

		abstract long IStorage.TotalOffsetInBytes { get; }

		void IStorage.Dispose(bool invokedByUser) { }

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
				throw new ArgumentException(ParameterError.UnexpectedType, nameof(storage));
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, ParameterError.CannotNegative);
			if (storage.LengthInBytes < offset + newLength)
				throw new ArgumentOutOfRangeException(nameof(newLength), newLength, ParameterError.InvalidValue);
			// return
			return (storage, offset, newLength);
		}

		/// <summary>
		/// Get the total offset compared to the start of the underlying reference in <typeparamref name="T"/>.
		/// </summary>
		/// <remarks>The default implementation does not check whether <see cref="IStorage.TotalOffsetInBytes"/> can be divided by <see cref="Unmanaged{T}.Size"/> or not.</remarks>
		public long TotalOffset => TotalOffsetInBytes / Unmanaged<T>.Size;
	}
}
