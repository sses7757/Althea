using System;

using Althea.Linq;


namespace Althea.Storage
{
	/// <summary>
	/// The managed pointer that implements <see cref="IPointer{TSelf}"/> as a example
	/// </summary>
	public readonly struct ManagedPointer : IPointer<ManagedPointer>
	{
		#region basic
		private readonly IntPtr data;

		/// <summary>
		/// Get the length of underly memory in bytes
		/// </summary>
		public long LengthInBytes { get; }

		/// <summary>
		/// Get the underlying data as a <see cref="Span{T}"/> of <see cref="byte"/>
		/// </summary>
		public unsafe Span<byte> Data => new(this.data.ToPointer(), (int)this.LengthInBytes);

		/// <summary>
		/// Get the underlying data as a <see cref="Span{T}"/> of <typeparamref name="T"/>
		/// </summary>
		public Span<T> AsData<T>() where T : unmanaged, INumber<T> => this.Data.As<byte, T>();

		/// <summary>
		/// Create a new <see cref="ManagedPointer"/> with given <paramref name="data"/> as a pointer from a fixed managed buffer and <paramref name="length"/>.
		/// </summary>
		/// <param name="data">The data as a pointer from a fixed managed buffer</param>
		/// <param name="length">The length of <paramref name="data"/> in bytes</param>
		public ManagedPointer(IntPtr data, long length)
		{
			this.data = data;
			this.LengthInBytes = length;
		}

		/// <summary>
		/// Check whether this pointer is valid or not
		/// </summary>
		public bool IsValid() => this.LengthInBytes > 0;

		static StorageLocation IPointer<ManagedPointer>.Location => new(LocationType.CpuRam, 0);

		static ManagedPointer IPointer<ManagedPointer>.Default => new();
		#endregion

		#region equality
		/// <summary>
		/// Check whether this <see cref="ManagedPointer"/> is the same as the <paramref name="other"/> one
		/// </summary>
		/// <param name="other">The other <see cref="ManagedPointer"/> to compare</param>
		/// <returns><c>this == <paramref name="other"/></c></returns>
		public bool Equals(ManagedPointer other) => this.data == other.data;

		/// <summary>
		/// Check whether this <see cref="ManagedPointer"/> is the same as the other <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">The other object to compare</param>
		/// <returns><c>this == <paramref name="obj"/></c></returns>
		public override bool Equals(object? obj) => obj is ManagedPointer p && this.Equals(p);

		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(ManagedPointer left, ManagedPointer right) => left.Equals(right);

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(ManagedPointer left, ManagedPointer right) => left.Equals(right);

		/// <summary>
		/// Get the hash code of this <see cref="ManagedPointer"/>
		/// </summary>
		public override int GetHashCode() => this.data.GetHashCode();
		#endregion

		#region string
		static string IMainPropertyFormattable<ManagedPointer>.StringMain => nameof(ManagedPointer);

		static IEnumerable<string> IMainPropertyFormattable<ManagedPointer>.PropertyNames => new[] { "FixedPointer", nameof(LengthInBytes) };

		IEnumerable<object?> IMainPropertyFormattable<ManagedPointer>.PropertyValues => new object[] { this.data, this.LengthInBytes };

		/// <summary>
		/// Get the string representation of this <see cref="ManagedPointer"/>
		/// </summary>
		public override string ToString() => IMainPropertyFormattable<ManagedPointer>.ToString(in this);
		#endregion
	}
}
