using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

using Althea.NativeTypes;
using Althea.Resources;

using Mem = Althea.Storage.ApiSelector;


namespace Althea.Storage
{
	/// <summary>
	/// The abstract storage class as a base class for all storage classes whose <see cref="IStorage.LocationDescription"/>.<see cref="CombinationOfLocations.Count">Count</see> == 1 and its <see cref="CombinationOfLocations.Type"/> == <see cref="CombinationType.AllStored"/>.
	/// </summary>
	/// <typeparam name="TP">Any pointer type which implements <see cref="IPointer{TSelf}"/></typeparam>
	/// <remarks>This class only servers as a type identifier which can not be used directly</remarks>
	public abstract class PureStorageBase<TP> where TP : notnull, IPointer<TP>
	{
		/// <summary>
		/// Get the <see cref="PointerSegment{T}"/> of this <see cref="PureStorageBase{TP}"/>
		/// </summary>
		public PointerSegment<TP> Pointer { get; }

		/// <summary>
		/// Create a new <see cref="PureStorageBase{TP}"/> with given <see cref="PointerSegment{T}"/> of type <typeparamref name="TP"/>
		/// </summary>
		/// <param name="pointer">The <see cref="PointerSegment{T}"/> of type <typeparamref name="TP"/> to create from</param>
		protected PureStorageBase(PointerSegment<TP> pointer)
		{
			this.Pointer = pointer;
		}
	}

	/// <summary>
	/// The abstract pure storage class that inherits <see cref="PureStorageBase{TP}"/> and constrains data type to <typeparamref name="T"/>
	/// </summary>
	/// <typeparam name="T">Any unmanaged number which implements <see cref="INumber{TSelf}"/> as the data type</typeparam>
	/// <typeparam name="TP">Any pointer type which implements <see cref="IPointer{TSelf}"/></typeparam>
	public abstract class PureStorage<T, TP> : PureStorageBase<TP>, IStorage<T, PureStorage<T, TP>> where T : unmanaged, INumber<T> where TP : notnull, IPointer<TP>
	{
		#region basic
		/// <summary>
		/// Create a new <see cref="PureStorage{T, TP}"/> with given <see cref="PointerSegment{T}"/> of type <typeparamref name="TP"/>
		/// </summary>
		/// <param name="pointer">The <see cref="PointerSegment{T}"/> of type <typeparamref name="TP"/> to create from</param>
		protected PureStorage(PointerSegment<TP> pointer) : base(pointer)
		{ }

		/// <summary>
		/// Statically get an empty <see cref="PureStorage{T, TP}"/>
		/// </summary>
		public static PureStorage<T, TP> Empty => new ReferencePureStorage<T, TP>(null);

		/// <summary>
		/// Statically get the data type of this storage as a <see cref="NativeTypes.DataType"/>
		/// </summary>
		public static DataType DataType => Unmanaged<T>.DataType;

		/// <summary>
		/// Statically get the description of the storage locations of this <see cref="PureStorage{T, TP}"/> as a <see cref="CombinationOfLocations"/>
		/// </summary>
		public static CombinationOfLocations LocationDescription => new(TP.Location);

#pragma warning disable CS8619
		static MethodInfo[] IStorage.PointerGetters => new[] { typeof(PureStorageBase<TP>).GetProperty(nameof(Pointer))?.GetGetMethod() };
#pragma warning restore CS8619

		long IStorage.SizeOfPointer(int i)
		{
			return this.IsValid() ? 1 : 0;
		}

		/// <summary>
		/// Get the total length of the presenting array in bytes
		/// </summary>
		public long LengthInBytes => this.Pointer.LengthInBytes;

		/// <summary>
		/// Get the total length of the presenting array in <typeparamref name="T"/>
		/// </summary>
		public long Length => ((IStorage<T, PureStorage<T, TP>>)this).Length;

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether this storage is disposed or not
		/// </summary>
		public bool Disposed { get; private set; } = false;

		void IStorage.Dispose(bool invokedByUser)
		{
			if (this is ActualPureStorage<T, TP>)
			{
				Mem.Free(this.Pointer.Pointer);
			}
			this.Disposed = true;
		}

		/// <summary>
		/// The deconstructor invoked by GC
		/// </summary>
		~PureStorage() => ((IStorage)this).Dispose(false);

		/// <summary>
		/// Check whether this <see cref="PureStorage{T, TP}"/> is a valid one or not
		/// </summary>
		/// <returns>The validness of this <see cref="PureStorage{T, TP}"/></returns>
		public bool IsValid() => !this.Disposed && this.Pointer.IsValid();
		#endregion

		#region reference
		/// <inheritdoc/>
		public bool OverlapWith(IStorage other)
		{
			return other is PureStorageBase<TP> s && this.Pointer.OverlapWith(s.Pointer);
		}

		/// <summary>
		/// Make a referenced <see cref="PureStorage{T, TP}"/> with the starting pointer moving <paramref name="offset"/> and <see cref="IStorage{T, TSelf}.Length"/> changing to <paramref name="newLength"/>.
		/// </summary>
		/// <param name="offset">The offset in <typeparamref name="T"/> to the starting pointer of this <see cref="PureStorage{T, TP}"/> as a <see cref="long"/></param>
		/// <param name="newLength">The new length in <typeparamref name="T"/> as a <see cref="long"/>, default 0 means automatically calculate from <paramref name="offset"/></param>
		/// <returns>A referenced <see cref="PureStorage{T, TP}"/> of this one</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> and <paramref name="newLength"/> is out of boundary</exception>
		public PureStorage<T, TP> MakeReference(long offset = 0, long newLength = 0)
		{
			if (offset == 0 && newLength == 0 && this is ReferencePureStorage<T, TP> @ref)
				return @ref;
			else
				return new ReferencePureStorage<T, TP>(this, offset, newLength);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static PureStorage<T, TP> IStorage<T, PureStorage<T, TP>>.RefFrom<TOut, TOther>(TOther storage)
		{
			return (storage as PureStorage<TOut, TP> ?? throw new InvalidOperationException(ParameterError.UnexpectedType)).As<T>();
		}

		/// <summary>
		/// Create a referenced storage of data type <typeparamref name="TOut"/> over this storage
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged number as the new data type</typeparam>
		/// <returns>The referenced <see cref="PureStorage{T, TP}"/> of data type <typeparamref name="TOut"/></returns>
		/// <exception cref="InvalidCastException">If the <see cref="LengthInBytes"/> cannot be divided by the size of <typeparamref name="TOut"/></exception>
		public PureStorage<TOut, TP> As<TOut>() where TOut : unmanaged, INumber<TOut>
		{
			if (typeof(TOut) == typeof(T))
				return this.MakeReference() as PureStorage<TOut, TP> ?? PureStorage<TOut, TP>.Empty;
			IStorage<T, PureStorage<T, TP>>.CheckCast<TOut>(this.Length);
			return new ReferencePureStorage<TOut, TP>(this);
		}
		#endregion

		#region create
		/// <summary>
		/// Statically <b>allocate</b> and create a new <see cref="PureStorage{T, TP}"/> of given lengths on different locations in <see cref="IStorage.LocationDescription"/>.
		/// </summary>
		/// <param name="lengths">The given lengths in <typeparamref name="T"/></param>
		/// <returns>The created new <see cref="PureStorage{T, TP}"/></returns>
		/// <exception cref="ArgumentException">If <paramref name="lengths"/> is not of size 1</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="lengths"/> has length(s) ≤ 0</exception>
		/// <exception cref="OutOfMemoryException">If the underlying allocation failed due to insufficient memory</exception>
		/// <exception cref="InvalidOperationException">If underlying creation fails due to other reasons</exception>
		public static PureStorage<T, TP> Create(ReadOnlySpan<long> lengths)
		{
			if (lengths.Length != 1)
				throw new ArgumentException(ParameterError.WrongSize, nameof(lengths));
			if (lengths[0] <= 0)
				throw new ArgumentOutOfRangeException(nameof(lengths), ParameterError.MustPositive);
			return new ActualPureStorage<T, TP>(lengths[0]);
		}

		static PureStorage<T, TP> IStorage<T, PureStorage<T, TP>>.CreateAlike<TOut, TOther>(TOther storage)
		{
			return CreateAlike(storage as PureStorage<TOut, TP> ?? throw new InvalidOperationException(ParameterError.UnexpectedType));
		}

		/// <summary>
		/// Statically allocate and creates a new <see cref="PureStorage{T, TP}"/> alike <paramref name="storage"/>.
		/// </summary>
		/// <param name="storage">The storage of data type <typeparamref name="TOut"/> to mimic.</param>
		/// <returns>A new <see cref="PureStorage{T, TP}"/> that likes <paramref name="storage"/></returns>
		public static PureStorage<T, TP> CreateAlike<TOut>(PureStorage<TOut, TP> storage) where TOut : unmanaged, INumber<TOut>
		{
			var descr = PureStorage<TOut, TP>.LocationDescription;
			return Create(stackalloc long[] { storage.Length });
		}
		#endregion

		#region operators
		static long IAdditiveIdentity<PureStorage<T, TP>, long>.AdditiveIdentity => 0;

		/// <summary>
		/// Indicates whether the current <see cref="PureStorage{T, TP}"/> is equal to the <paramref name="other"/> <see cref="PureStorage{T, TP}"/> of the same type.
		/// </summary>
		/// <param name="other">The other <see cref="PureStorage{T, TP}"/> to compare to</param>
		/// <returns>true if the current <see cref="PureStorage{T, TP}"/> is equal to the <paramref name="other"/>; otherwise, false.</returns>
		public bool Equals(PureStorage<T, TP>? other) => other is not null && this.Pointer == other.Pointer;

		/// <summary>
		/// Get the hash code of this <see cref="PureStorage{T, TP}"/>
		/// </summary>
		/// <returns>The hash code of this <see cref="PureStorage{T, TP}"/></returns>
		public override int GetHashCode() => this.Pointer.GetHashCode();

		/// <summary>
		/// Check whether this <see cref="PureStorage{T, TP}"/> equals the other <paramref name="obj"/> or not
		/// </summary>
		/// <param name="obj">The other object to compare to</param>
		/// <returns><c>this == <paramref name="obj"/></c></returns>
		public override bool Equals(object? obj) => this.Equals(obj as PureStorage<T, TP>);

		/// <summary>
		/// Statically get the distance in <typeparamref name="T"/> between two <see cref="PureStorage{T, TP}"/>s
		/// </summary>
		/// <param name="left">The left operand of type <see cref="PureStorage{T, TP}"/></param>
		/// <param name="right">The right operand of type <see cref="PureStorage{T, TP}"/></param>
		/// <returns>The distance between two <see cref="PureStorage{T, TP}"/>s in <typeparamref name="T"/> as a <see cref="long"/>.</returns>
		/// <exception cref="InvalidOperationException">If <paramref name="left"/> and <paramref name="right"/> have different origin.</exception>
		public static long operator -(PureStorage<T, TP> left, PureStorage<T, TP> right)
		{
			long diffBytes = IStorage<T, PureStorage<T, TP>>.StorageDiffBytes(left, right);
			if (diffBytes % Unmanaged<T>.Size != 0)
				throw new InvalidOperationException(ArithmeticError.CannotDivide);
			return diffBytes / Unmanaged<T>.Size;
		}

		/// <summary>
		/// <see cref="PureStorage{T, TP}"/> addition operator
		/// </summary>
		public static PureStorage<T, TP> operator +(PureStorage<T, TP> left, long right) => left.MakeReference(right);

		/// <summary>
		/// <see cref="PureStorage{T, TP}"/> subtraction operator
		/// </summary>
		public static PureStorage<T, TP> operator -(PureStorage<T, TP> left, long right) => left.MakeReference(-right);

		/// <summary>
		/// <see cref="PureStorage{T, TP}"/> equality operator
		/// </summary>
		public static bool operator ==(PureStorage<T, TP> left, PureStorage<T, TP> right) => left.Equals(right);

		/// <summary>
		/// <see cref="PureStorage{T, TP}"/> inequality operator
		/// </summary>
		public static bool operator !=(PureStorage<T, TP> left, PureStorage<T, TP> right) => !left.Equals(right);
		#endregion

		#region string
		static string IMainPropertyFormattable<PureStorage<T, TP>>.StringMain => nameof(PureStorage<T, TP>);

		static IEnumerable<string> IMainPropertyFormattable<PureStorage<T, TP>>.PropertyNames => new[] { nameof(DataType), nameof(Length), nameof(Pointer) };

		IEnumerable<object?> IMainPropertyFormattable<PureStorage<T, TP>>.PropertyValues => new object[] { DataType, this.Length, this.Pointer };

		/// <inheritdoc/>
		public override string ToString() => IMainPropertyFormattable<PureStorage<T, TP>>.ToString(this);

		static JsonConverter<PureStorage<T, TP>> IStorage<T, PureStorage<T, TP>>.JsonConverter => new JsonConverter();

		private sealed class JsonConverter : JsonConverter<PureStorage<T, TP>>
		{
			private record struct Repr(string Data);

			public override PureStorage<T, TP>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				////var tempOptions = new JsonSerializerOptions(options) { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
				if (reader.TokenType != JsonTokenType.StartObject || !reader.Read())
					throw new JsonException();
				if (reader.TokenType != JsonTokenType.PropertyName || reader.GetString() != nameof(Repr.Data) || !reader.Read())
					throw new JsonException();
				if (reader.TokenType != JsonTokenType.String)
					throw new JsonException();

				byte[] data = reader.GetBytesFromBase64();
				TP pointer = Mem.Allocate<TP>(data.LongLength);
				Mem.FromManaged<byte, TP>(pointer, data);
				
				if (!reader.Read())
					throw new JsonException();
				if (reader.TokenType != JsonTokenType.EndObject)
					throw new JsonException();
				reader.Read();

				return new ActualPureStorage<T, TP>(pointer);
			}

			public override void Write(Utf8JsonWriter writer, PureStorage<T, TP> value!!, JsonSerializerOptions options)
			{
				if (!value.IsValid())
					throw new JsonException(ParameterError.InvalidValue);
				byte[] temp = new byte[value.LengthInBytes];
				Mem.ToManaged<byte, TP>(value.Pointer, temp);
				writer.WriteStartObject();
				writer.WriteBase64String(nameof(Repr.Data), temp);
				writer.WriteEndObject();
			}
		}
		#endregion
	}

	/// <summary>
	/// The actual storage class for a pure storage on a single location.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number which implements <see cref="INumber{TSelf}"/> as the data type</typeparam>
	/// <typeparam name="TP">Any pointer type which implements <see cref="IPointer{TSelf}"/></typeparam>
	public sealed class ActualPureStorage<T, TP> : PureStorage<T, TP>, IActualStorage<T, PureStorage<T, TP>>
		where T : unmanaged, INumber<T>
		where TP : notnull, IPointer<TP>
	{
		internal ActualPureStorage(TP pointer) : base(pointer)
		{
			// do nothing
		}

		/// <summary>
		/// Create a new <see cref="ActualPureStorage{T, TP}"/> of given <paramref name="length"/>
		/// </summary>
		/// <param name="length">The length to create in <typeparamref name="T"/></param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="length"/> ≤ 0</exception>
		/// <exception cref="OutOfMemoryException">If <paramref name="length"/> is too large to be allocated</exception>
		public ActualPureStorage(long length) : base(length > 0 ? Mem.Allocate<TP>(length * Unmanaged<T>.Size) : throw new ArgumentOutOfRangeException(nameof(length), ParameterError.MustPositive))
		{
			// do nothing
		}
	}

	/// <summary>
	/// The reference storage class for a pure storage on a single location.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number which implements <see cref="INumber{TSelf}"/> as the data type</typeparam>
	/// <typeparam name="TP">Any pointer type which implements <see cref="IPointer{TSelf}"/></typeparam>
	public sealed class ReferencePureStorage<T, TP> : PureStorage<T, TP>, IReferenceStorage<T, PureStorage<T, TP>>
		where T : unmanaged, INumber<T>
		where TP : notnull, IPointer<TP>
	{
		/// <summary>
		/// Get the reference <see cref="IStorage"/> of this <see cref="ReferencePureStorage{T, TP}"/>
		/// </summary>
		public IStorage? Reference { get; }

		/// <summary>
		/// Get the total offset of this <see cref="ReferencePureStorage{T, TP}"/> in bytes
		/// </summary>
		public long TotalOffsetInBytes => this.Pointer.OffsetInBytes;

		/// <summary>
		/// Create a new <see cref="ReferencePureStorage{T, TP}"/> from given base <paramref name="storage"/> and <paramref name="offset"/> and <paramref name="newLength"/>.
		/// </summary>
		/// <param name="storage">The base <see cref="IStorage"/> to refer to</param>
		/// <param name="offset">The offset in <typeparamref name="T"/> compared to <paramref name="storage"/></param>
		/// <param name="newLength">The new presenting length in <typeparamref name="T"/></param>
		/// <exception cref="ArgumentException">If <paramref name="storage"/> is not a <see cref="PureStorageBase{TP}"/></exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> and <paramref name="newLength"/> are out of boundary</exception>
		public ReferencePureStorage(IStorage? storage, long offset = 0, long newLength = 0) :
			base(storage is PureStorageBase<TP> p ? p.Pointer.MoveBy(offset * Unmanaged<T>.Size, newLength * Unmanaged<T>.Size) : default)
		{
			var (reference, _, _) = IReferenceStorage<T, PureStorage<T, TP>>.Create<PureStorageBase<TP>>(storage, offset, newLength);
			this.Reference = reference;
		}
	}
}
