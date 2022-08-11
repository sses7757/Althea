using System.Reflection;
using System.Text.Json.Serialization;

using Althea.Helpers;


namespace Althea.Storage
{
	/// <summary>
	/// The managed pointer that implements <see cref="IPointer{TSelf}"/> as a example
	/// </summary>
	public readonly record struct ManagedPointer(IntPtr Pointer, long LengthInBytes) : IPointer<ManagedPointer>
	{
		#region basic
		/// <summary>
		/// Get the underlying data as a <see cref="Span{T}"/> of <see cref="byte"/>
		/// </summary>
		public unsafe Span<byte> Data => new(this.Pointer.ToPointer(), (int)this.LengthInBytes);

		/// <summary>
		/// Get the underlying data as a <see cref="Span{T}"/> of <typeparamref name="T"/>
		/// </summary>
		public Span<T> AsData<T>() where T : unmanaged, IBaseNumber<T> => this.Data.As<byte, T>();

		/// <inheritdoc/>
		public bool IsValid() => this.LengthInBytes > 0;

		static StorageLocation IPointer<ManagedPointer>.Location => new(LocationType.CpuRam, 0);

		static ManagedPointer IPointer<ManagedPointer>.Default => new();
		#endregion
	}

	internal sealed class ManagedPureStorage<T> : IStorage<T, ManagedPureStorage<T>> where T : unmanaged, IBaseNumber<T>
	{
		#region basic
		IStorage? IStorage.Reference => null;

		long IStorage.TotalOffsetInBytes => 0;

		static JsonConverter<ManagedPureStorage<T>> IStorage<T, ManagedPureStorage<T>>.JsonConverter => null!;

		public PointerSegment<ManagedPointer> Pointer { get; }

		internal ManagedPureStorage()
		{
			Pointer = default;
		}

		public ManagedPureStorage(ManagedPointer mp)
		{
			this.Pointer = mp;
		}

		public static ManagedPureStorage<T> Empty => new();

		public static DataType DataType => T.Type;

		public static CombinationOfLocations LocationDescription => new StorageLocation(LocationType.CpuRam, 0);

		public static string StringMain => typeof(ManagedPureStorage<T>).GetGenericString();

		public static IEnumerable<string> PropertyNames => new[] { nameof(Pointer) };

		static long System.Numerics.IAdditiveIdentity<ManagedPureStorage<T>, long>.AdditiveIdentity => 0;

		public long LengthInBytes => this.Pointer.LengthInBytes;

		public long Length => this.Pointer.LengthInBytes / T.Size;

		public IEnumerable<object?> PropertyValues => new object[] { this.Pointer  };

		bool IStorage.Disposed => false;

		void IStorage.Dispose(bool invokedByUser)
		{
			// do nothing
		}

		static MethodInfo[] IStorage<ManagedPureStorage<T>>.PointerGetters => new[] { typeof(ManagedPureStorage<T>).GetProperty(nameof(Pointer))!.GetGetMethod()! };

		public static ManagedPureStorage<T> Create(ReadOnlySpan<long> lengths) => throw new InvalidOperationException();

		static ManagedPureStorage<T> IStorage<T, ManagedPureStorage<T>>.CreateAlike<TOut, TOther>(TOther storage) => throw new InvalidOperationException();

		static ManagedPureStorage<T> IStorage<T, ManagedPureStorage<T>>.RefFrom<TOut, TOther>(TOther storage) => throw new InvalidOperationException();

		public bool Equals(ManagedPureStorage<T>? other) => other is not null && other.Pointer == this.Pointer;

		public bool IsValid() => this.Pointer.IsValid();

		public ManagedPureStorage<T> MakeReference(long offset = 0, long newLength = 0) => throw new InvalidOperationException();

		public static ManagedPureStorage<T> operator +(ManagedPureStorage<T> left, long offset) => left.MakeReference(offset);

		public static long operator -(ManagedPureStorage<T> left, ManagedPureStorage<T> right) => throw new InvalidOperationException();

		public static ManagedPureStorage<T> operator -(ManagedPureStorage<T> left, long offset) => left.MakeReference(-offset);

		public static bool operator ==(ManagedPureStorage<T> left, ManagedPureStorage<T> right) => left.Equals(right);

		public static bool operator !=(ManagedPureStorage<T> left, ManagedPureStorage<T> right) => !left.Equals(right);

		public override bool Equals(object? obj) => this.Equals(obj as ManagedPureStorage<T>);

		public override int GetHashCode() => this.Pointer.GetHashCode();

		ReadOnlySpan<long> IStorage<T, ManagedPureStorage<T>>.GetPointerSizes(Span<long> sizes)
		{
			if (sizes.Length < 1)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(sizes));
			sizes[0] = this.Pointer.LengthInBytes;
			return sizes[..1];
		}

		bool IStorage.OverlapWith(IStorage other)
		{
			return other is ManagedPureStorage<T> mp && this.Pointer.OverlapWith(mp.Pointer);
		}
		#endregion
	}
}
