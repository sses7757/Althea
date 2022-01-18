using System.Runtime.CompilerServices;

using Althea.Linq;
using Althea.NativeTypes;
using Althea.Resources;

using MEM = Althea.Storage.AbstractApi;


namespace Althea.Storage
{
	/// <summary>
	/// The abstract storage class as a base class for all storage classes whose <see cref="IStorage.LocationDescription"/>.<see cref="CombinationOfLocations.Count">Count</see> == 2 and its <see cref="CombinationOfLocations.Type"/> == <see cref="CombinationType.AllStored"/>.
	/// </summary>
	/// <typeparam name="TP1">The first pointer type which implements <see cref="IPointer{TSelf}"/></typeparam>
	/// <typeparam name="TP2">The second pointer type which implements <see cref="IPointer{TSelf}"/></typeparam>
	/// <remarks>This class only servers as a type identifier which can not be used directly</remarks>
	public abstract class MixedStorageBase<TP1, TP2> where TP1 : notnull, IPointer<TP1> where TP2 : notnull, IPointer<TP2>
	{
		/// <summary>
		/// Get the first <see cref="PointerSegment{T}"/> of type <typeparamref name="TP1"/> of this storage
		/// </summary>
		public PointerSegment<TP1> Pointer1 { get; protected init; }

		/// <summary>
		/// Get the second <see cref="PointerSegment{T}"/> of type <typeparamref name="TP2"/> of this storage
		/// </summary>
		public PointerSegment<TP2> Pointer2 { get; protected init; }

		/// <summary>
		/// Create a new <see cref="MixedStorageBase{TP1, TP2}"/> with given <see cref="PointerSegment{T}"/>s
		/// </summary>
		/// <param name="pointer1">The <see cref="PointerSegment{T}"/> of type <typeparamref name="TP1"/> to create from</param>
		/// <param name="pointer2">The <see cref="PointerSegment{T}"/> of type <typeparamref name="TP2"/> to create from</param>
		protected MixedStorageBase(PointerSegment<TP1> pointer1, PointerSegment<TP2> pointer2)
		{
			this.Pointer1 = pointer1;
			this.Pointer2 = pointer2;
		}

		/// <summary>
		/// Create an empty <see cref="MixedStorageBase{TP1, TP2}"/> with <see cref="PointerSegment{T}"/>s to be set later by inherited classes
		/// </summary>
		protected MixedStorageBase() { }
	}

	/// <summary>
	/// The abstract mixed storage class that inherits <see cref="MixedStorageBase{TP1, TP2}"/> and constrains data type to <typeparamref name="T"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct which implements <see cref="INumber{TSelf}"/> as the data type</typeparam>
	/// <typeparam name="TP1">The first pointer type which implements <see cref="IPointer{TSelf}"/></typeparam>
	/// <typeparam name="TP2">The second pointer type which implements <see cref="IPointer{TSelf}"/></typeparam>
	public abstract class MixedStorage<T, TP1, TP2> : MixedStorageBase<TP1, TP2>, IStorage<T, MixedStorage<T, TP1, TP2>>
		where T : unmanaged, INumber<T>
		where TP1 : notnull, IPointer<TP1> where TP2 : notnull, IPointer<TP2>
	{
		#region basic
		/// <summary>
		/// Create a new <see cref="MixedStorage{T, TP1, TP2}"/> with given <see cref="PointerSegment{T}"/>s
		/// </summary>
		/// <param name="pointer1">The <see cref="PointerSegment{T}"/> of type <typeparamref name="TP1"/> to create from</param>
		/// <param name="pointer2">The <see cref="PointerSegment{T}"/> of type <typeparamref name="TP2"/> to create from</param>
		protected MixedStorage(PointerSegment<TP1> pointer1, PointerSegment<TP2> pointer2) : base(pointer1, pointer2)
		{ }

		/// <summary>
		/// Create a new <see cref="MixedStorage{T, TP1, TP2}"/> with given <see cref="PointerSegment{T}"/>s
		/// </summary>
		/// <param name="pointers">The <see cref="PointerSegment{T}"/>s of type <typeparamref name="TP1"/> and <typeparamref name="TP2"/> to create from</param>
		protected MixedStorage((PointerSegment<TP1> pointer1, PointerSegment<TP2> pointer2) pointers) : base(pointers.pointer1, pointers.pointer2)
		{ }

		/// <summary>
		/// Create an empty <see cref="MixedStorage{T, TP1, TP2}"/> with <see cref="PointerSegment{T}"/>s to be set later by inherited classes
		/// </summary>
		protected MixedStorage() : base() { }

		/// <summary>
		/// Statically get an empty <see cref="MixedStorage{T, TP1, TP2}"/>
		/// </summary>
		public static MixedStorage<T, TP1, TP2> Empty => new ReferenceMixedStorage<T, TP1, TP2>(null);

		/// <summary>
		/// Statically get the data type of this storage as a <see cref="NativeTypes.DataType"/>
		/// </summary>
		public static DataType DataType => NativeType<T>.DataType;

		/// <summary>
		/// Statically get the description of the storage locations of this <see cref="MixedStorage{T, TP1, TP2}"/> as a <see cref="CombinationOfLocations"/>
		/// </summary>
		public static CombinationOfLocations LocationDescription => new(CombinationType.AllStored, stackalloc[] { TP1.Location, TP2.Location });

		/// <summary>
		/// Get the total length of the presenting array in bytes
		/// </summary>
		public long LengthInBytes => Pointer1.LengthInBytes + Pointer2.LengthInBytes;

		/// <summary>
		/// Get the total length of the presenting array in <typeparamref name="T"/>
		/// </summary>
		public long Length => this.LengthInBytes / NativeType<T>.Size;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Dispose(bool invokedByUser)
		{
			if (this is ActualMixedStorage<T, TP1, TP2>)
			{
				MEM.Free(this.Pointer1.Pointer, invokedByUser);
				MEM.Free(this.Pointer2.Pointer, invokedByUser);
			}
		}

		void IStorage.Dispose(bool invokedByUser) => this.Dispose(invokedByUser);

		/// <summary>
		/// The deconstructor invoked by GC
		/// </summary>
		~MixedStorage() => this.Dispose(false);

		/// <summary>
		/// Check whether this <see cref="MixedStorage{T, TP1, TP2}"/> is a valid one or not
		/// </summary>
		/// <returns>The validness of this <see cref="MixedStorage{T, TP1, TP2}"/></returns>
		public bool IsValid() => this.Pointer1.IsValid() || this.Pointer2.IsValid();
		#endregion

		#region reference
		/// <summary>
		/// Make a referenced <see cref="MixedStorage{T, TP1, TP2}"/> with the starting pointer moving <paramref name="offset"/> and <see cref="IStorage{T, TSelf}.Length"/> changing to <paramref name="newLength"/>.
		/// </summary>
		/// <param name="offset">The offset in <typeparamref name="T"/> to the starting pointer of this <see cref="MixedStorage{T, TP1, TP2}"/> as a <see cref="long"/></param>
		/// <param name="newLength">The new length in <typeparamref name="T"/> as a <see cref="long"/>, default 0 means automatically calculate from <paramref name="offset"/></param>
		/// <returns>A referenced <see cref="MixedStorage{T, TP1, TP2}"/> of this one</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> and <paramref name="newLength"/> is out of boundary</exception>
		public MixedStorage<T, TP1, TP2> MakeReference(long offset = 0, long newLength = 0)
		{
			if (offset == 0 && newLength == 0 && this is ReferenceMixedStorage<T, TP1, TP2> @ref)
				return @ref;
			else
				return new ReferenceMixedStorage<T, TP1, TP2>(this, offset, newLength);
		}

		/// <summary>
		/// Check whether this <see cref="MixedStorage{T, TP1, TP2}"/> overlaps with the <paramref name="other"/> <see cref="MixedStorage{T, TP1, TP2}"/>.
		/// </summary>
		/// <param name="other">The other <see cref="MixedStorage{T, TP1, TP2}"/> to check overlap</param>
		/// <returns>True if this overlaps with the <paramref name="other"/>, false otherwise</returns>
		/// <remarks>This method does not consider a rather case that pointers can be of same type.</remarks>
		public bool OverlapWith(MixedStorage<T, TP1, TP2> other) => this.Pointer1.OverlapWith(other.Pointer1) || this.Pointer2.OverlapWith(other.Pointer2);

		static MixedStorage<T, TP1, TP2> IStorage<T, MixedStorage<T, TP1, TP2>>.RefFrom<TOut, TOther>(TOther storage)
		{
			return (storage as MixedStorage<TOut, TP1, TP2> ?? throw new InvalidOperationException(Parameter.UnexpectedType)).As<T>();
		}

		/// <summary>
		/// Create a referenced storage of data type <typeparamref name="TOut"/> over this storage
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged number as the new data type</typeparam>
		/// <returns>The referenced <see cref="MixedStorage{T, TP1, TP2}"/> of data type <typeparamref name="TOut"/></returns>
		/// <exception cref="InvalidCastException">If the <see cref="LengthInBytes"/> cannot be divided by the size of <typeparamref name="TOut"/></exception>
		public MixedStorage<TOut, TP1, TP2> As<TOut>() where TOut : unmanaged, INumber<TOut>
		{
			if (typeof(TOut) == typeof(T))
				return this.MakeReference() as MixedStorage<TOut, TP1, TP2> ?? MixedStorage<TOut, TP1, TP2>.Empty;
			IStorage<T, MixedStorage<T, TP1, TP2>>.CheckCast<TOut>(this.Length);
			return new ReferenceMixedStorage<TOut, TP1, TP2>(this);
		}
		#endregion

		#region create
		/// <summary>
		/// Statically <b>allocate</b> and create a new <see cref="MixedStorage{T, TP1, TP2}"/> of given <paramref name="combinationType"/> and given locations and lengths.
		/// </summary>
		/// <param name="combinationType">The given <see cref="CombinationType"/> to create</param>
		/// <param name="locations">The given <see cref="StorageLocation"/>s</param>
		/// <param name="lengths">The given lengths in <typeparamref name="T"/></param>
		/// <returns>The created new <see cref="MixedStorage{T, TP1, TP2}"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="locations"/> or <paramref name="lengths"/> is null or empty</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="lengths"/> has length(s) ≤ 0</exception>
		/// <exception cref="OutOfMemoryException">If the underlying allocation failed due to insufficient memory</exception>
		/// <exception cref="InvalidOperationException">If underlying creation fails due to other reasons</exception>
		public static MixedStorage<T, TP1, TP2> Create(CombinationType combinationType, ReadOnlySpan<StorageLocation> locations, ReadOnlySpan<long> lengths)
		{
			if (combinationType != CombinationType.AllStored || locations.Length != 2 || lengths.Length != 2)
				throw new InvalidOperationException(Support.Location);
			if (lengths[0] <= 0 || lengths[1] <= 0)
				throw new ArgumentOutOfRangeException(nameof(lengths), Parameter.MustPositive);
			TP1 pointer1 = MEM.Allocate<T>(locations[0], lengths[0]);
			TP2 pointer2 = MEM.Allocate<T>(locations[1], lengths[1]);
			return new ActualMixedStorage<T, TP1, TP2>(pointer1, pointer2);
		}


		static MixedStorage<T, TP1, TP2> IStorage<T, MixedStorage<T, TP1, TP2>>.CreateAlike<TOut, TOther>(TOther storage)
		{
			return CreateAlike(storage as MixedStorage<TOut, TP1, TP2> ?? throw new InvalidOperationException(Parameter.UnexpectedType));
		}

		/// <summary>
		/// Statically allocate and creates a new <see cref="MixedStorage{T, TP1, TP2}"/> alike <paramref name="storage"/>.
		/// </summary>
		/// <param name="storage">The storage of data type <typeparamref name="TOut"/> to mimic.</param>
		/// <returns>A new <see cref="MixedStorage{T, TP1, TP2}"/> that likes <paramref name="storage"/></returns>
		public static MixedStorage<T, TP1, TP2> CreateAlike<TOut>(MixedStorage<TOut, TP1, TP2> storage) where TOut : unmanaged, INumber<TOut>
		{
			var descr = MixedStorage<TOut, TP1, TP2>.LocationDescription;
			return Create(descr.Type, descr.CopyLocationsToSpan(stackalloc StorageLocation[1]), stackalloc long[1] { storage.Length });
		}
		#endregion

		#region operators
		static long IAdditiveIdentity<MixedStorage<T, TP1, TP2>, long>.AdditiveIdentity => 0;

		/// <summary>
		/// Indicates whether the current <see cref="MixedStorage{T, TP1, TP2}"/> is equal to the <paramref name="other"/> <see cref="MixedStorage{T, TP1, TP2}"/> of the same type.
		/// </summary>
		/// <param name="other">The other <see cref="MixedStorage{T, TP1, TP2}"/> to compare to</param>
		/// <returns>true if the current <see cref="MixedStorage{T, TP1, TP2}"/> is equal to the <paramref name="other"/>; otherwise, false.</returns>
		public bool Equals(MixedStorage<T, TP1, TP2>? other) => other is not null && this.Pointer1 == other.Pointer1 && this.Pointer2 == other.Pointer2;

		/// <summary>
		/// Get the hash code of this <see cref="MixedStorage{T, TP1, TP2}"/>
		/// </summary>
		/// <returns>The hash code of this <see cref="MixedStorage{T, TP1, TP2}"/></returns>
		public override int GetHashCode() => HashCode.Combine(this.Pointer1, this.Pointer2);

		/// <summary>
		/// Check whether this <see cref="MixedStorage{T, TP1, TP2}"/> equals the other <paramref name="obj"/> or not
		/// </summary>
		/// <param name="obj">The other object to compare to</param>
		/// <returns><c>this == <paramref name="obj"/></c></returns>
		public override bool Equals(object? obj) => this.Equals(obj as MixedStorage<T, TP1, TP2>);

		/// <summary>
		/// Statically get the distance in <typeparamref name="T"/> between two <see cref="MixedStorage{T, TP1, TP2}"/>s
		/// </summary>
		/// <param name="left">The left operand of type <see cref="MixedStorage{T, TP1, TP2}"/></param>
		/// <param name="right">The right operand of type <see cref="MixedStorage{T, TP1, TP2}"/></param>
		/// <returns>The distance between two <see cref="MixedStorage{T, TP1, TP2}"/>s in <typeparamref name="T"/> as a <see cref="long"/>.</returns>
		/// <exception cref="InvalidOperationException">If <paramref name="left"/> and <paramref name="right"/> have different origin.</exception>
		public static long operator -(MixedStorage<T, TP1, TP2> left, MixedStorage<T, TP1, TP2> right)
		{
			long diffBytes = IStorage<T, MixedStorage<T, TP1, TP2>>.StorageDiffBytes(left, right);
			if (diffBytes % NativeType<T>.Size != 0)
				throw new InvalidOperationException(Other.CannotDivide);
			return diffBytes / NativeType<T>.Size;
		}

		/// <summary>
		/// <see cref="MixedStorage{T, TP1, TP2}"/> addition operator
		/// </summary>
		public static MixedStorage<T, TP1, TP2> operator +(MixedStorage<T, TP1, TP2> left, long right) => left.MakeReference(right);

		/// <summary>
		/// <see cref="MixedStorage{T, TP1, TP2}"/> subtraction operator
		/// </summary>
		public static MixedStorage<T, TP1, TP2> operator -(MixedStorage<T, TP1, TP2> left, long right) => left.MakeReference(-right);

		/// <summary>
		/// <see cref="MixedStorage{T, TP1, TP2}"/> equality operator
		/// </summary>
		public static bool operator ==(MixedStorage<T, TP1, TP2> left, MixedStorage<T, TP1, TP2> right) => left.Equals(right);

		/// <summary>
		/// <see cref="MixedStorage{T, TP1, TP2}"/> inequality operator
		/// </summary>
		public static bool operator !=(MixedStorage<T, TP1, TP2> left, MixedStorage<T, TP1, TP2> right) => !left.Equals(right);
		#endregion

		#region string
		static string IMainPropertyFormattable<MixedStorage<T, TP1, TP2>>.StringMain => nameof(MixedStorage<T, TP1, TP2>);

		static IEnumerable<string> IMainPropertyFormattable<MixedStorage<T, TP1, TP2>>.PropertyNames => new[] { nameof(DataType), nameof(IStorage<T, MixedStorage<T, TP1, TP2>>.Length), nameof(Pointer1), nameof(Pointer2) };

		IEnumerable<object?> IMainPropertyFormattable<MixedStorage<T, TP1, TP2>>.PropertyValues => new object?[] { DataType, ((IStorage<T, MixedStorage<T, TP1, TP2>>)this).Length, this.Pointer1.Pointer.ToString(), this.Pointer2.Pointer.ToString() };

		/// <summary>
		/// Return the string representation of this <see cref="MixedStorage{T, TP1, TP2}"/>
		/// </summary>
		/// <returns>the string representation of this <see cref="MixedStorage{T, TP1, TP2}"/></returns>
		public override string ToString() => IMainPropertyFormattable<MixedStorage<T, TP1, TP2>>.ToString(this);
		#endregion
	}

	/// <summary>
	/// The actual storage class for a mixed storage on two locations.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct which implements <see cref="INumber{TSelf}"/> as the data type</typeparam>
	/// <typeparam name="TP1">The first pointer type which implements <see cref="IPointer{TSelf}"/></typeparam>
	/// <typeparam name="TP2">The second pointer type which implements <see cref="IPointer{TSelf}"/></typeparam>
	public sealed class ActualMixedStorage<T, TP1, TP2> : MixedStorage<T, TP1, TP2>, IActualStorage<T, MixedStorage<T, TP1, TP2>>
		where T : unmanaged, INumber<T>
		where TP1 : notnull, IPointer<TP1> where TP2 : notnull, IPointer<TP2>
	{
		/// <summary>
		/// Create a new <see cref="ActualMixedStorage{T, TP1, TP2}"/> from the given <paramref name="pointer1"/> of type <typeparamref name="TP1"/>, <paramref name="pointer2"/> of type <typeparamref name="TP2"/>
		/// </summary>
		/// <param name="pointer1">The given pointer of type <typeparamref name="TP1"/></param>
		/// <param name="pointer2">The given pointer of type <typeparamref name="TP2"/></param>
		public ActualMixedStorage(TP1 pointer1, TP2 pointer2) : base(new(pointer1), new(pointer2))
		{
			// do nothing
		}
	}

	/// <summary>
	/// The reference storage class for a pure storage on a single location.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct which implements <see cref="INumber{TSelf}"/> as the data type</typeparam>
	/// <typeparam name="TP1">The first pointer type which implements <see cref="IPointer{TSelf}"/></typeparam>
	/// <typeparam name="TP2">The second pointer type which implements <see cref="IPointer{TSelf}"/></typeparam>
	public sealed class ReferenceMixedStorage<T, TP1, TP2> : MixedStorage<T, TP1, TP2>, IReferenceStorage<T, MixedStorage<T, TP1, TP2>>
		where T : unmanaged, INumber<T>
		where TP1 : notnull, IPointer<TP1> where TP2 : notnull, IPointer<TP2>
	{
		/// <summary>
		/// Get the reference <see cref="IStorage"/> of this <see cref="ReferenceMixedStorage{T, TP1, TP2}"/>
		/// </summary>
		public IStorage? Reference { get; }

		/// <summary>
		/// Get the total offset of this <see cref="ReferenceMixedStorage{T, TP1, TP2}"/> in bytes compared to <see cref="Reference"/>
		/// </summary>
		public long TotalOffsetInBytes { get; }

		/// <summary>
		/// Create a new <see cref="ReferenceMixedStorage{T, TP1, TP2}"/> from given base <paramref name="storage"/> and <paramref name="offset"/> and <paramref name="newLength"/>.
		/// </summary>
		/// <param name="storage">The base <see cref="IStorage"/> to refer to</param>
		/// <param name="offset">The offset in <typeparamref name="T"/> compared to <paramref name="storage"/></param>
		/// <param name="newLength">The new presenting length in <typeparamref name="T"/></param>
		/// <exception cref="ArgumentException">If <paramref name="storage"/> is not a <see cref="PureStorageBase{TP}"/></exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> and <paramref name="newLength"/> are out of boundary</exception>
		public ReferenceMixedStorage(IStorage? storage, long offset = 0, long newLength = 0)
		{
			(storage, offset, newLength) = IReferenceStorage<T, MixedStorage<T, TP1, TP2>>.Create<MixedStorageBase<TP1, TP2>>(storage, offset, newLength);
			if (storage is not MixedStorageBase<TP1, TP2> s)
				return;

			this.Reference = storage; this.TotalOffsetInBytes = offset;

			PointerSegment<TP1> p1 = default; PointerSegment<TP2> p2 = default;
			long offsetEnd = offset + newLength;
			Span<long> lenAccu = stackalloc[] { s.Pointer1.LengthInBytes, s.Pointer2.LengthInBytes };
			lenAccu.AccumulateSum(lenAccu, inclusive: false);
			int firstNonEmpty = lenAccu.UpperBound(offset), lastNonEmpty = lenAccu.LowerBound(offsetEnd);

			if (0 > firstNonEmpty && 0 < lastNonEmpty)
				p1 = s.Pointer1;
			else if (0 == firstNonEmpty && 0 == lastNonEmpty)
				p1 = s.Pointer1.MoveBy(offset, newLength);
			else if (0 == firstNonEmpty && 0 < lastNonEmpty)
				p1 = s.Pointer1.MoveBy(offset);
			else if (0 > firstNonEmpty && 0 == lastNonEmpty)
				p1 = s.Pointer1.MoveBy(0, newLength - lenAccu[lastNonEmpty - 1]);

			offset -= s.Pointer1.LengthInBytes;
			if (1 > firstNonEmpty && 1 < lastNonEmpty)
				p2 = s.Pointer2;
			else if (1 == firstNonEmpty && 1 == lastNonEmpty)
				p2 = s.Pointer2.MoveBy(offset, newLength);
			else if (1 == firstNonEmpty && 1 < lastNonEmpty)
				p2 = s.Pointer2.MoveBy(offset);
			else if (1 > firstNonEmpty && 1 == lastNonEmpty)
				p2 = s.Pointer2.MoveBy(0, newLength - lenAccu[lastNonEmpty - 1]);

			this.Pointer1 = p1; this.Pointer2 = p2;
		}
	}
}
