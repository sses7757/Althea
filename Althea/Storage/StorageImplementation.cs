using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.Linq;
using Althea.Resources;

using MEM = Althea.Storage.AbstractApi;


namespace Althea.Storage
{
	#region enum
	/// <summary>
	/// The enum representing the URI schemes which can be used as memories.
	/// </summary>
	/// <remarks>See <see cref="Uri.UriSchemeFile"/>, etc.</remarks>
	public enum UriScheme : int
	{
		/// <summary>
		/// Specifies that the URI scheme is unknown
		/// </summary>
		Unknown = 0,
		/// <summary>
		/// Specifies that the URI is a pointer to a file
		/// </summary>
		File = 1,
		/// <summary>
		/// Specifies that the URI is accessed through the TCP/IP directly.
		/// </summary>
		TCP = 2,
		/// <summary>
		/// Specifies that the URI is accessed through the File Transfer Protocol (FTP).
		/// </summary>
		FTP = 3,
		/// <summary>
		/// Specifies that the URI is accessed through the Hypertext Transfer Protocol (HTTP).
		/// </summary>
		HTTP = 4,
		/// <summary>
		/// Specifies that the URI is accessed through the Secure Hypertext Transfer Protocol (HTTPS).
		/// </summary>
		HTTPS = 5,
	}

	/// <summary>
	/// The static class for extension methods of <see cref="UriScheme"/>
	/// </summary>
	public static class UriSchemeExtension
	{
		private static readonly Dictionary<UriScheme, string> static_names = new Dictionary<UriScheme, string>();

		private static readonly Dictionary<string, UriScheme> static_names_inv = new Dictionary<string, UriScheme>();

		/// <summary>
		/// Set the name (string representation) of given <see cref="UriScheme"/>
		/// </summary>
		/// <param name="scheme">The given <see cref="UriScheme"/> whose name will be set</param>
		/// <param name="name">The name (string representation) of given <paramref name="scheme"/></param>
		/// <returns>If <paramref name="scheme"/> is a pre-defined one,
		/// or <paramref name="name"/> is null or contains space or non-ASCII characters,
		/// or <paramref name="name"/> already exists,
		/// this method returns false;
		/// otherwise, the name will be set and this method returns true</returns>
		public static bool SetName(this UriScheme scheme, string name)
		{
			if (scheme >= UriScheme.Unknown && scheme <= UriScheme.HTTPS)
				return false;
			if (name is null || name.Contains(' ') || Encoding.UTF8.GetByteCount(name) != name.Length) // check ASCII
				return false;
			if (static_names_inv.ContainsKey(name))
				return false;
			static_names[scheme] = name;
			static_names_inv[name] = scheme;
			return true;
		}

		/// <summary>
		/// Get the name (string representation) of given <see cref="UriScheme"/> (can be the ones preset by <see cref="SetName(UriScheme, string)"/>
		/// </summary>
		/// <param name="scheme">The given <see cref="UriScheme"/> whose name will be get</param>
		/// <returns>The name (string representation) of <paramref name="scheme"/> or the underlying number if the name cannot be obtained</returns>
		public static string GetName(this UriScheme scheme)
		{
			if (static_names.ContainsKey(scheme))
				return static_names[scheme];
			else
				return scheme.ToString();
		}

		/// <summary>
		/// Get the <see cref="UriScheme"/> from a <see cref="Uri"/>
		/// </summary>
		/// <param name="uri">the absolute <see cref="Uri"/></param>
		/// <returns>the <see cref="UriScheme"/> of <paramref name="uri"/>, or <see cref="UriScheme.Unknown"/> if <paramref name="uri"/>'s scheme is not in <see cref="UriScheme"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="uri"/> is not an absolute URI</exception>
		public static UriScheme GetScheme(this Uri uri)
		{
			if (!uri.IsAbsoluteUri)
				throw new ArgumentOutOfRangeException(nameof(uri));
			if (uri.Scheme == Uri.UriSchemeFile)
				return UriScheme.File;
			if (uri.Scheme == @"tcp" || uri.Scheme == Uri.UriSchemeNetTcp)
				return UriScheme.TCP;
			if (uri.Scheme == Uri.UriSchemeFtp)
				return UriScheme.FTP;
			if (uri.Scheme == Uri.UriSchemeHttp)
				return UriScheme.HTTP;
			if (uri.Scheme == Uri.UriSchemeHttps)
				return UriScheme.HTTPS;
			if (static_names_inv.ContainsKey(uri.Scheme))
				return static_names_inv[uri.Scheme];
			return UriScheme.Unknown;
		}
	}
	#endregion


	#region interfaces
	/// <summary>
	/// The interface for an immutable pointer at any possible memory storage which can be described by a <see cref="IntPtr"/>
	/// </summary>
	public interface IMemoryPointer : IPointer
	{
		#region basic
		/// <summary>
		/// When implemented by a derived class, get the raw pointer of this <see cref="IMemoryPointer"/> as a <see cref="IntPtr"/>
		/// </summary>
		IntPtr Pointer { get; }

		bool ICheckValid.IsValid() => this.Pointer != default && this.LengthInBytes != 0;

		string IMainPropertyFormat.StringMain => this.Pointer.ToString("X");

		IReadOnlyDictionary<string, string> IMainPropertyFormat.StringProperties => new Dictionary<string, string> { ["length"] = this.LengthInBytes.ToString() };
		#endregion

		#region default implementations
		/// <summary>
		/// Get the unmanaged pointer of this <see cref="IMemoryPointer"/> with given offset as a <typeparamref name="T"/>*
		/// </summary>
		/// <typeparam name="T">any unmanaged data type</typeparam>
		/// <param name="offset">The offset in <typeparamref name="T"/> to the <see cref="NativePointer"/> of this <see cref="IMemoryPointer"/></param>
		/// <returns>The unmanaged pointer with given offset as a <typeparamref name="T"/>*</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		unsafe T* UnmangedPointer<T>(long offset = 0) where T : unmanaged => (T*)this.Pointer.ToPointer() + offset;

		/// <summary>
		/// Get the native pointer of this <see cref="IMemoryPointer"/> with a given offset as a <see cref="void"/>*
		/// </summary>
		/// <param name="offset">The offset in bytes to the <see cref="Pointer"/> of this <see cref="IMemoryPointer"/></param>
		/// <returns>The native pointer with given offset as a <see cref="void"/>*</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		unsafe void* NativePointer(long offset = 0) => (byte*)this.Pointer.ToPointer() + offset;

		/// <summary>
		/// Get the <see cref="Span{T}"/> representation of this <see cref="IMemoryPointer"/> with given offset and length
		/// </summary>
		/// <typeparam name="T">any unmanaged data type</typeparam>
		/// <param name="offset">The offset in <typeparamref name="T"/> to the <see cref="Pointer"/> of this <see cref="IMemoryPointer"/> before converting to <see cref="Span{T}"/></param>
		/// <param name="length">The presenting length in <typeparamref name="T"/> of this <see cref="IMemoryPointer"/> before converting to <see cref="Span{T}"/></param>
		/// <returns>The <see cref="Span{T}"/> representation of this <see cref="IMemoryPointer"/></returns>
		/// <remarks>If the underlying memory of this <see cref="IMemoryPointer"/> is not on managed or unmanaged heap of current program, any operation to the return of this method may throw error or give unexpected results. Therefore, this method is set to be protected internal.</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected internal unsafe Span<T> AsSpan<T>(long offset = 0, int length = 0) where T : unmanaged => new Span<T>(this.UnmangedPointer<T>(offset), length == 0 ? checked((int)this.LengthInBytes / sizeof(T)) : length);

		/// <summary>
		/// Get the <see cref="Span{T}"/> representation of this <see cref="IMemoryPointer"/> with given offset and length
		/// </summary>
		/// <typeparam name="T">any unmanaged data type</typeparam>
		/// <param name="pointerSegment">Use the given <see cref="PointerSegment"/> to obtain offset and length</param>
		/// <returns>The <see cref="Span{T}"/> representation of this <see cref="IMemoryPointer"/></returns>
		/// <remarks>If the underlying memory of this <see cref="IMemoryPointer"/> is not on managed or unmanaged heap of current program, any operation to the return of this method may throw error or give unexpected results. Therefore, this method is set to be protected internal.</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected internal Span<T> AsSpan<T>(PointerSegment pointerSegment) where T : unmanaged => this.AsSpan<T>(checked((long)(pointerSegment.OffsetInBytes / Storage<T>.SizeOfT)), checked((int)(pointerSegment.LengthInBytes / Storage<T>.SizeOfT)));
		#endregion
	}

	/// <summary>
	/// The abstract class for any possible stream storage
	/// </summary>
	public abstract class Stream : IDisposable, ICheckValid
	{
		#region basic
		/// <summary>
		/// Whether this class is disposed or not
		/// </summary>
		protected bool Disposed { get; private set; } = false;

		/// <summary>
		/// When implemented by a derived class, actually release the unmanaged (and possibly managed) resources held by this class
		/// </summary>
		/// <param name="disposeManaged">Dispose managed resources or not</param>
		protected abstract void Dispose(bool disposeManaged);

		/// <summary>
		/// Dispose the unmanaged and managed resources held by this class
		/// </summary>
		public void Dispose()
		{
			this.Dispose(disposeManaged: true);
			this.Disposed = true;
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Check whether this object is a valid one or not
		/// </summary>
		/// <returns>The validness of this object</returns>
		public bool IsValid() => !this.Disposed && this.Length != 0;

		/// <summary>
		/// When implemented by a derived class, get or set the position (offset) in bytes of this <see cref="Stream"/>
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">If the value to be set is not less than <see cref="Length"/></exception>
		public abstract ulong Position { get; set; }

		/// <summary>
		/// When implemented by a derived class, get or set the length in bytes of this <see cref="Stream"/>
		/// </summary>
		public ulong Length { get; }

		/// <summary>
		/// Create a <see cref="Stream"/> with given <paramref name="length"/> in bytes
		/// </summary>
		/// <param name="length">The given length in bytes</param>
		protected Stream(ulong length) => this.Length = length;

		/// <summary>
		/// When implemented by a derived class, get a <see cref="bool"/> indicating whether this <see cref="Stream"/> can transfer data with managed C# memory directly or not.
		/// </summary>
		public abstract bool CanTransferWithManaged { get; }

		/// <summary>
		/// When implemented by a derived class, <b>statically</b> get the supported data transfer locations represented by <see cref="StorageLocation"/>s of this <see cref="Stream"/>
		/// </summary>
		/// <remarks>When implemented by a derived class, if this property returns null or empty list, <see cref="NullReferenceException"/> may be thrown</remarks>
		public abstract IReadOnlyList<StorageLocation> SupportedTransfers { get; }

		/// <summary>
		/// When implemented by a derived class, <b>statically</b> get a <see cref="bool"/> indicating whether data transfer with given <paramref name="location"/> is supported by this <see cref="Stream"/>. The default implementation utilizes the <see cref="SupportedTransfers"/>.
		/// </summary>
		/// <param name="location">The given <see cref="StorageLocation"/> to check transfer supporting</param>
		/// <returns>Whether data transfer with <paramref name="location"/> is supported or not</returns>
		public virtual bool IsSupported(StorageLocation location) => this.SupportedTransfers.Contains(location);

		/// <summary>
		/// When implemented by a derived class, get the string representation of this <see cref="Stream"/>.
		/// </summary>
		/// <returns>The string representation of this <see cref="Stream"/></returns>
		public abstract override string ToString();
		#endregion

		#region read and write
		/// <summary>
		/// When overridden in a derived class, clears all buffers for this stream and causes any buffered data to be written to the underlying device.
		/// </summary>
		/// <exception cref="System.IO.IOException">If a general I/O error occurred</exception>
		/// <exception cref="ObjectDisposedException">If this is already disposed</exception>
		public abstract void Flush();

		/// <summary>
		/// When implemented by a derived class, read data from this <see cref="Stream"/> started from <see cref="Position"/> byte and write them to the given <see cref="PointerSegment"/> <paramref name="memory"/>.
		/// </summary>
		/// <param name="memory">The <see cref="PointerSegment"/> to write to</param>
		/// <remarks>When finished, the <see cref="Position"/> shall be advanced by the number of bytes read.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="memory"/> is not valid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="memory"/>.<see cref="PointerSegment.LengthInBytes">Length</see> exceeds the boundary of this <see cref="Stream"/></exception>
		/// <exception cref="NotSupportedException">If <paramref name="memory"/>.<see cref="PointerSegment.Location">Location</see> is not supported</exception>
		/// <exception cref="System.IO.IOException">If a general I/O error occurred</exception>
		/// <exception cref="ObjectDisposedException">If this is already disposed</exception>
		public abstract void ToMemory(PointerSegment memory);

		/// <summary>
		/// When implemented by a derived class, read data from this <see cref="Stream"/> started from <see cref="Position"/> and write them to the given <paramref name="managed"/> memory as a<see cref="Span{T}"/>.
		/// </summary>
		/// <param name="managed">The managed memory as a <see cref="Span{T}"/> to write into</param>
		/// <remarks>When finished, the <see cref="Position"/> shall be advanced by the number of bytes read.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="managed"/> is not valid (for example, has zero length)</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="managed"/>'s length exceeds the boundary</exception>
		/// <exception cref="NotSupportedException">If <see cref="CanTransferWithManaged"/> is false</exception>
		/// <exception cref="System.IO.IOException">If a general I/O error occurred</exception>
		/// <exception cref="ObjectDisposedException">If this is already disposed</exception>
		public abstract void ToManged<T>(Span<T> managed) where T : unmanaged;

		/// <summary>
		/// When implemented by a derived class, read data from the given <see cref="PointerSegment"/> <paramref name="memory"/> and write them to this <see cref="Stream"/> started from <see cref="Position"/> byte.
		/// </summary>
		/// <param name="memory">The <see cref="PointerSegment"/> to read from</param>
		/// <remarks>When finished, the <see cref="Position"/> shall be advanced by the number of bytes written.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="memory"/> is not valid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="memory"/>.<see cref="PointerSegment.LengthInBytes">Length</see> exceeds the boundary of this <see cref="Stream"/></exception>
		/// <exception cref="NotSupportedException">If <paramref name="memory"/>.<see cref="PointerSegment.Location">Location</see> is not supported</exception>
		/// <exception cref="System.IO.IOException">If a general I/O error occurred</exception>
		/// <exception cref="ObjectDisposedException">If this is already disposed</exception>
		public abstract void FromMemory(PointerSegment memory);

		/// <summary>
		/// When implemented by a derived class, read data from the given <paramref name="managed"/> memory as a<see cref="Span{T}"/> and write them to this <see cref="Stream"/> started from <see cref="Position"/>.
		/// </summary>
		/// <param name="managed">The managed memory as a <see cref="Span{T}"/> to read from</param>
		/// <remarks>When finished, the <see cref="Position"/> shall be advanced by the number of bytes written.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="managed"/> is not valid (for example, has zero length)</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="managed"/>'s length exceeds the boundary</exception>
		/// <exception cref="NotSupportedException">If <see cref="CanTransferWithManaged"/> is false</exception>
		/// <exception cref="System.IO.IOException">If a general I/O error occurred</exception>
		/// <exception cref="ObjectDisposedException">If this is already disposed</exception>
		public abstract void FromManged<T>(Span<T> managed) where T : unmanaged;
		#endregion

		#region default implementation
		/// <summary>
		/// Get the default buffer size in bytes which is divisible by the size of <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">any unmanaged data type</typeparam>
		/// <returns>The default buffer size in bytes divisible by the size of <typeparamref name="T"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static uint BufferSizeInBytes<T>() where T : unmanaged => (1 << 16) / Storage<T>.SizeOfT * Storage<T>.SizeOfT;

		private static readonly Dictionary<Type, StorageLocation> cache_single_location = new Dictionary<Type, StorageLocation>();

		/// <summary>
		/// When overridden in a derived class, fill some values of this <see cref="Stream"/> of given <paramref name="length"/>. The default implementation tries to use the managed buffer or buffer allocated on the found first intersection of both <see cref="SupportedTransfers"/>.
		/// </summary>
		/// <typeparam name="T">any unmanaged data type</typeparam>
		/// <param name="value">The value of type <typeparamref name="T"/> to be set</param>
		/// <param name="length">The length in <typeparamref name="T"/></param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="length"/> exceeds any of the boundaries</exception>
		/// <exception cref="System.IO.IOException">If an I/O error occurs</exception>
		/// <exception cref="ObjectDisposedException">If this is already disposed</exception>
		public virtual void SetValues<T>(T value, ulong length) where T : unmanaged
		{
			if (length == 0)
				throw new ArgumentOutOfRangeException(nameof(length), Parameter.MustPositive);
			if (this.Disposed)
				throw new ObjectDisposedException(this.GetType().FullName);

			uint bufferSize = BufferSizeInBytes<T>() / Storage<T>.SizeOfT;
			if (this.CanTransferWithManaged)
			{
				int len = checked((int)length);
				T[] buffer = new T[Math.Min(bufferSize, len)];
				Array.Fill(buffer, value);
				while (len > 0)
				{
					var span = buffer.AsSpan(0, Math.Min(len, buffer.Length));
					this.FromManged(span);
					len -= span.Length;
				}
			}
			else
			{
				// get StorageLocation cache
				var key = this.GetType();
				StorageLocation location;
				if (cache_single_location.ContainsKey(key))
				{
					location = cache_single_location[key];
				}
				else
				{
					cache_single_location.Add(key, this.SupportedTransfers[0]);
					location = this.SupportedTransfers[0];
				}
				// copy
				ulong len = Math.Min(bufferSize, length);
				var impl = MEM.SelectImplementation(location);
				var pointer = impl.Allocate(location, len);
				try
				{
					impl.SetMemoryValue(pointer, value);
					while (len > 0)
					{
						if (len < pointer.LengthInBytes)
							pointer = pointer.AsLength(len);
						this.FromMemory(pointer);
						len -= pointer.LengthInBytes;
					}
				}
				finally
				{
					impl.Free(pointer);
				}
			}
			// flush at the end
			this.Flush();
		}

		private static readonly Dictionary<ImmutableTwoElementSet<Type>, StorageLocation> cache_double_location = new Dictionary<ImmutableTwoElementSet<Type>, StorageLocation>();

		/// <summary>
		/// When overridden in a derived class, copy some data from this <see cref="Stream"/> to <paramref name="other"/> <see cref="Stream"/> of given <paramref name="length"/>. The default implementation tries to use the managed buffer or buffer allocated on the found first intersection of both <see cref="SupportedTransfers"/>.
		/// </summary>
		/// <param name="other">The other <see cref="Stream"/> to copy to</param>
		/// <param name="length">The length in bytes to copy</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="length"/> exceeds any of the boundaries</exception>
		/// <exception cref="NotSupportedException">If there are not common supported data transfers between this and <paramref name="other"/></exception>
		/// <exception cref="System.IO.IOException">If an I/O error occurs</exception>
		/// <exception cref="ObjectDisposedException">If this or <paramref name="other"/> is already disposed</exception>
		public virtual void CopyTo(Stream other, ulong length)
		{
			if (length == 0)
				throw new ArgumentOutOfRangeException(nameof(length), Parameter.MustPositive);
			if (this.Disposed)
				throw new ObjectDisposedException(this.GetType().FullName);
			if (this.Position + length > this.Length)
				throw new ArgumentOutOfRangeException(nameof(length), Parameter.InvalidValue);
			if (other.Position + length > other.Length)
				throw new ArgumentOutOfRangeException(nameof(length), Parameter.InvalidValue);

			uint bufferSize = BufferSizeInBytes<byte>();
			if (this.CanTransferWithManaged && other.CanTransferWithManaged)
			{
				int len = checked((int)length);
				byte[] buffer = new byte[Math.Min(bufferSize, len)];
				while (len > 0)
				{
					var span = buffer.AsSpan(0, Math.Min(len, buffer.Length));
					this.ToManged(span);
					other.FromManged(span);
					len -= span.Length;
				}
			}
			else
			{
				// get StorageLocation cache
				var key = new ImmutableTwoElementSet<Type>(this.GetType(), other.GetType());
				StorageLocation value;
				if (cache_double_location.ContainsKey(key))
				{
					value = cache_double_location[key];
				}
				else
				{
					var intersect = this.SupportedTransfers.Intersect(other.SupportedTransfers);
					if (intersect.Count == 0)
						throw new NotSupportedException(Support.Location);
					cache_double_location.Add(key, intersect[0]);
					value = intersect[0];
				}
				// copy
				ulong len = Math.Min(bufferSize, length);
				var impl = MEM.SelectImplementation(value);
				var pointer = impl.Allocate(value, len);
				try
				{
					while (len > 0)
					{
						if (len < pointer.LengthInBytes)
							pointer = pointer.AsLength(len);
						this.ToMemory(pointer);
						other.FromMemory(pointer);
						len -= pointer.LengthInBytes;
					}
				}
				finally
				{
					impl.Free(pointer);
				}
			}
			// flush at the end
			other.Flush();
		}
		#endregion
	}

	/// <summary>
	/// The interface for an immutable pointer at any possible stream storage which can be described by a <see cref="Stream"/>
	/// </summary>
	public interface IStreamPointer : IPointer, IDisposable
	{
		#region basic
		/// <summary>
		/// When implemented by a derived class, get the native stream this <see cref="IStreamPointer"/> as a <see cref="Stream"/>.
		/// </summary>
		public Stream NativeStream { get; }
		#endregion

		#region implemented interface methods
		ulong IPointer.LengthInBytes => this.NativeStream.Length;

		bool ICheckValid.IsValid() => this.NativeStream.IsValid();

		IReadOnlyDictionary<string, string> IMainPropertyFormat.StringProperties => new Dictionary<string, string>
		{ 
			["length"] = this.NativeStream.Length.ToString(),
			["position"] = this.NativeStream.Position.ToString(),
		};
		#endregion
	}
	#endregion


	#region pure or mixed storage classes
	/// <summary>
	/// The interface for any cached storage, including actual ones and referenced one
	/// </summary>
	public interface IPureOrMixedStorage : IStorage { }

	/// <summary>
	/// The abstract storage class as a base class for all <see cref="Storage{T}"/> classes with <see cref="Storage{T}.LocationDescription"/>.<see cref="CombinationOfLocations.Type">Type</see> == <see cref="CombinationType.PureOrMixed"/>. Inherits <see cref="ActualStorage{T}"/>.
	/// </summary>
	/// <typeparam name="T">any unmanaged data type</typeparam>
	public abstract class PureOrMixedStorage<T> : ActualStorage<T>, IPureOrMixedStorage where T : unmanaged
	{
		#region basic
		/// <summary>
		/// The description of the storage locations of this storage class as a <see cref="CombinationOfLocations"/>
		/// </summary>
		public override CombinationOfLocations LocationDescription { get; }

		/// <summary>
		/// Create (without allocation) a <see cref="PureOrMixedStorage{T}"/> with given <paramref name="locations"/> and <paramref name="lengths"/>
		/// </summary>
		/// <param name="locations">The locations as a <see cref="ReadOnlySpan{T}"/> of <see cref="StorageLocation"/></param>
		/// <param name="lengths">The presenting lengths in <typeparamref name="T"/> as a <see cref="ReadOnlySpan{T}"/> of <see cref="ulong"/></param>
		/// <exception cref="ArgumentNullException">If the sizes of <paramref name="locations"/> or <paramref name="lengths"/> is 0</exception>
		/// <exception cref="ArgumentException">If the sizes of <paramref name="locations"/> and <paramref name="lengths"/> are not the same</exception>
		/// <exception cref="ArgumentOutOfRangeException">If any length in <paramref name="lengths"/> is 0</exception>
		protected PureOrMixedStorage(ReadOnlySpan<StorageLocation> locations, ReadOnlySpan<ulong> lengths) : base(lengths.Sum())
		{
			if (locations.Length == 0)
				throw new ArgumentNullException(nameof(locations));
			if (lengths.Length == 0)
				throw new ArgumentNullException(nameof(lengths));
			if (locations.Length != lengths.Length)
				throw new ArgumentException(Parameter.NotSameSize);
			if (lengths.Any(l => l == 0))
				throw new ArgumentOutOfRangeException(nameof(lengths), Parameter.MustPositive);

			this.LocationDescription = new CombinationOfLocations(CombinationType.PureOrMixed, locations);
		}
		#endregion

		#region override
		/// <summary>
		/// Make a <see cref="PureOrMixedReferenceStorage{T}"/> with the starting pointer moving <paramref name="offset"/> and <see cref="Storage{T}.Length"/> changing to <paramref name="newLength"/>.
		/// </summary>
		/// <param name="offset">The offset in <typeparamref name="T"/> to the starting pointer of this <see cref="Storage{T}"/> as a <see cref="long"/></param>
		/// <param name="newLength">The new length in <typeparamref name="T"/> as a <see cref="ulong"/>, default 0 means automatically calculate from <paramref name="offset"/></param>
		/// <returns>A <see cref="PureOrMixedReferenceStorage{T}"/> of this one</returns>
		public override PureOrMixedReferenceStorage<T> MakeReference(long offset = 0, ulong newLength = 0)
		{
			return new PureOrMixedReferenceStorage<T>(this, offset, newLength);
		}

		/// <summary>
		/// Convert this <see cref="PureOrMixedStorage{T}"/> to another one with different data type <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">the output data type</typeparam>
		/// <returns>A <see cref="PureOrMixedReferenceStorage{TOut}"/></returns>
		/// <exception cref="InvalidCastException">if <see cref="IStorage.LengthInBytes"/> cannot be divided by <see cref="Storage{TOut}.SizeOfT"/></exception>
		public override PureOrMixedReferenceStorage<TOut> As<TOut>()
		{
			ulong newLength = CheckCast<TOut>(this.Length);
			return new PureOrMixedReferenceStorage<TOut>(this, newLength: newLength);
		}
		#endregion
	}

	/// <summary>
	/// The storage class that references to a <see cref="PureOrMixedStorage{T}"/>, implements <see cref="ReferenceStorage{T}"/>
	/// </summary>
	/// <typeparam name="T">any unmanaged data type</typeparam>
	public class PureOrMixedReferenceStorage<T> : ReferenceStorage<T>, IReferenceStorage, IPureOrMixedStorage where T : unmanaged
	{
		#region basic
		private readonly int start, end;

		private readonly ulong startOffsetBytes, endOffsetBytes;

		/// <summary>
		/// Get the number of <see cref="PointerSegment"/>(s) of this <see cref="Storage{T}"/> 
		/// </summary>
		public override int Count => this.end - this.start;

		/// <summary>
		/// Get the description of the storage locations of this <see cref="Storage{T}"/> class as a <see cref="CombinationOfLocations"/>
		/// </summary>
		public override CombinationOfLocations LocationDescription => this.Reference?.LocationDescription[this.start..this.end] ?? default;

		/// <summary>
		/// Create a <see cref="PureOrMixedReferenceStorage{T}"/> with given reference <paramref name="storage"/> and <paramref name="offset"/> to it
		/// </summary>
		/// <param name="storage">the <see cref="PureOrMixedStorage{T}"/> to be referenced</param>
		/// <param name="offset">the total offset in <typeparamref name="T"/> as a <see cref="long"/></param>
		/// <param name="newLength">the new presenting length in <typeparamref name="T"/>, default 0 means automatically calculate by <paramref name="storage"/> and <paramref name="offset"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="storage"/> or its reference is null</exception>
		/// <exception cref="ArgumentException">If <paramref name="storage"/> is not a <see cref="PureOrMixedStorage{T}"/></exception>
		public PureOrMixedReferenceStorage(IStorage storage, long offset = 0, ulong newLength = 0) : base(storage, offset, newLength)
		{
			if (this.Reference is null)
				return;
			// check 
			if (this.Reference is not PureOrMixedStorage<T> && !this.Reference.GetType().MakeGenericType(typeof(T)).IsAssignableTo(typeof(PureOrMixedStorage<T>)))
				throw new ArgumentException(Parameter.UnexpectedType, nameof(storage));
			// set offsets
			ulong offsetInBytes = this.TotalOffsetInBytes;
			for (int i = 0; i < storage.Count; i++)
			{
				ulong lengthOfI = storage[i].LengthInBytes;
				if (offsetInBytes < lengthOfI)
				{   // set
					this.start = i; this.startOffsetBytes = offsetInBytes;
					break;
				}
				else
				{
					offsetInBytes -= lengthOfI;
				}
			}
			offsetInBytes += this.LengthInBytes;
			for (int i = this.start; i < storage.Count; i++)
			{
				ulong lengthOfI = storage[i].LengthInBytes;
				if (offsetInBytes <= lengthOfI)
				{   // set
					this.end = i + 1; this.endOffsetBytes = offsetInBytes;
					break;
				}
				else
				{
					offsetInBytes -= lengthOfI;
				}
			}
		}

		private PureOrMixedReferenceStorage(IStorage? storage, long offset, ulong newLength, int start, int end, ulong startOffsetBytes, ulong endOffsetBytes) : base(storage, offset, newLength)
		{
			this.start = start; this.end = end;
			this.startOffsetBytes = startOffsetBytes; this.endOffsetBytes = endOffsetBytes;
		}
		#endregion

		#region override
		/// <summary>
		/// Indexer of the <see cref="PointerSegment"/>(s) of this <see cref="PureOrMixedReferenceStorage{T}"/> (in presenting order)
		/// </summary>
		/// <param name="index">the element index</param>
		/// <returns>the <see cref="PointerSegment"/> at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of the range</exception>
		/// <exception cref="InvalidOperationException">If the referenced storage of this <see cref="PureOrMixedReferenceStorage{T}"/> is null</exception>
		public override PointerSegment this[int index] {
			get {
				if (index < 0 || index >= this.Count)
					throw new ArgumentOutOfRangeException(nameof(index));
				if (this.Reference is null)
					throw new InvalidOperationException();
				PointerSegment pointer = this.Reference[index - this.start];
				if (index == 0)
				{
					pointer = pointer.MoveBy((long)this.startOffsetBytes);
				}
				else if (index == this.Count - 1)
				{   // this.Count cannot be 1
					pointer = new PointerSegment(pointer, newLength: this.endOffsetBytes);
				}
				return pointer;
			}
		}

		/// <summary>
		/// Make a <see cref="ReferenceStorage{T}"/> with the starting pointer moving <paramref name="offset"/> and <see cref="Storage{T}.Length"/> changing to <paramref name="newLength"/>.
		/// </summary>
		/// <param name="offset">The offset in <typeparamref name="T"/> to the starting pointer of this <see cref="Storage{T}"/> as a <see cref="long"/></param>
		/// <param name="newLength">The new length in <typeparamref name="T"/> as a <see cref="ulong"/>, default 0 means automatically calculate from <paramref name="offset"/></param>
		/// <returns>A <see cref="PureOrMixedReferenceStorage{T}"/> of this one</returns>
		public override PureOrMixedReferenceStorage<T> MakeReference(long offset = 0, ulong newLength = 0)
		{
			return new PureOrMixedReferenceStorage<T>(this, offset, newLength);
		}

		/// <summary>
		/// Convert this <see cref="PureOrMixedReferenceStorage{T}"/> to another one with different data type <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">the output data type</typeparam>
		/// <returns>a referenced <see cref="PureOrMixedReferenceStorage{TOut}"/></returns>
		/// <exception cref="InvalidCastException">if <see cref="IStorage.LengthInBytes"/> cannot be divided by <see cref="Storage{TOut}.SizeOfT"/></exception>
		public override PureOrMixedReferenceStorage<TOut> As<TOut>()
		{
			if (this.Reference is null)
				return new PureOrMixedReferenceStorage<TOut>(null, 0, 0, 0, 0, 0, 0);
			long offset = CheckCast<TOut>((long)this.TotalOffsetInBytes, sizeInBytes: true);
			ulong length = CheckCast<TOut>(this.Reference.LengthInBytes - this.TotalOffsetInBytes, sizeInBytes: true);
			return new PureOrMixedReferenceStorage<TOut>(this.Reference, offset, length);
		}

		/// <summary>
		/// Determines whether the specified object is equal to the current object.
		/// </summary>
		/// <param name="obj">another object</param>
		/// <returns>this equals to <paramref name="obj"/> or not</returns>
		public override bool Equals(Storage<T>? obj)
		{
			if (obj is not null && obj is PureOrMixedReferenceStorage<T> @ref)
			{
				return this.Reference == @ref.Reference && this.start == @ref.start && this.end == @ref.end && this.startOffsetBytes == @ref.startOffsetBytes && this.endOffsetBytes == @ref.endOffsetBytes;
			}
			return false;
		}

		/// <summary>
		/// Get the hash code of this <see cref="ReferenceStorage{T}"/>
		/// </summary>
		/// <returns>the hash code</returns>
		public override int GetHashCode() => HashCode.Combine(this.Reference, this.start, this.end, this.startOffsetBytes, this.endOffsetBytes);
		#endregion
	}

	/// <summary>
	/// Represents a storage of a contiguous memory block on a certain memory location, implements <see cref="PureOrMixedStorage{T}"/>
	/// </summary>
	/// <typeparam name="T">any unmanaged data type</typeparam>
	public class PureStorage<T> : PureOrMixedStorage<T> where T : unmanaged
	{
		#region basic
		private readonly PointerSegment pointer;

		/// <summary>
		/// <b>Allocate</b> and create a <see cref="PureStorage{T}"/> of given <see cref="Storage{T}.Length"/> on given <see cref="StorageLocation"/> 
		/// </summary>
		/// <param name="location">a <see cref="StorageLocation"/> to represent the memory location</param>
		/// <param name="length">the length of contiguous memory block in <typeparamref name="T"/></param>
		public PureStorage(StorageLocation location, ulong length) : base(SpanLinq.AsSpan(ref location), SpanLinq.AsSpan(ref length))
		{
			this.pointer = Allocate(location, length);
		}
		#endregion

		#region override
		/// <summary>
		/// The number of <see cref="PointerSegment"/>(s) of this <see cref="Storage{T}"/> 
		/// </summary>
		public override int Count => 1;

		/// <summary>
		/// Indexer of the <see cref="PointerSegment"/>(s) of this <see cref="Storage{T}"/> (in presenting order)
		/// </summary>
		/// <param name="index">the element index</param>
		/// <returns>the <see cref="PointerSegment"/> at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range</exception>
		public override PointerSegment this[int index] {
			get {
				if (index != 0)
					throw new ArgumentOutOfRangeException(nameof(index));
				return this.pointer;
			}
		}
		#endregion
	}

	/// <summary>
	/// Represents a storage of several contiguous memory blocks on different memory locations with fixed sizes, implements <see cref="ActualStorage{T}"/>
	/// </summary>
	/// <typeparam name="T">any unmanaged data type</typeparam>
	public class MixedStorage<T> : PureOrMixedStorage<T> where T : unmanaged
	{
		#region basic
		private readonly PointerSegment[] pointers;

		/// <summary>
		/// <b>Allocate</b> and create a <see cref="MixedStorage{T}"/> of given lengths on given <see cref="StorageLocation"/>s
		/// </summary>
		/// <param name="locations">the <see cref="ReadOnlySpan{T}"/> of given <see cref="StorageLocation"/>s</param>
		/// <param name="lengths">the <see cref="ReadOnlySpan{T}"/> of given lengths</param>
		/// <param name="allowSameLocation">allow same <see cref="StorageLocation"/>s in <paramref name="locations"/> or not</param>
		/// <exception cref="ArgumentNullException">If the sizes of <paramref name="locations"/> or <paramref name="lengths"/> is 0</exception>
		/// <exception cref="ArgumentException">If the sizes of <paramref name="locations"/> and <paramref name="lengths"/> are not the same; or <paramref name="allowSameLocation"/> is false while <paramref name="locations"/> contains duplicate value(s)</exception>
		/// <exception cref="ArgumentOutOfRangeException">If any length in <paramref name="lengths"/> is 0</exception>
		public MixedStorage(ReadOnlySpan<StorageLocation> locations, ReadOnlySpan<ulong> lengths, bool allowSameLocation = true) : base(locations, lengths)
		{
			if (!allowSameLocation && !locations.ElementsUnique())
				throw new ArgumentException(Parameter.DuplicateValue, nameof(locations));

			try
			{
				this.pointers = new PointerSegment[locations.Length];
				for (int i = 0; i < locations.Length; i++)
				{
					this.pointers[i] = Allocate(locations[i], lengths[i]);
				}
			}
			catch (Exception)
			{
				this.Dispose(true);
				throw;
			}
		}

		/// <summary>
		/// <b>Allocate</b> and create a <see cref="MixedStorage{T}"/> of given lengths on given <see cref="StorageLocation"/>s
		/// </summary>
		/// <param name="param">the <see cref="Array"/> of given lengths and <see cref="StorageLocation"/>s</param>
		public MixedStorage(params (StorageLocation location, ulong length)[] param) :
			this(param.CopyTo(stackalloc StorageLocation[param.Length], static p => p.location),
				 param.CopyTo(stackalloc ulong[param.Length], static p => p.length),
				 allowSameLocation: true)
		{ }
		#endregion

		#region override
		/// <summary>
		/// The number of <see cref="PointerSegment"/>(s) of this <see cref="Storage{T}"/> 
		/// </summary>
		public override int Count => this.pointers.Length;

		/// <summary>
		/// Indexer of the <see cref="PointerSegment"/>(s) of this <see cref="Storage{T}"/> (in presenting order)
		/// </summary>
		/// <param name="index">the element index</param>
		/// <returns>the <see cref="PointerSegment"/> at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range</exception>
		public override PointerSegment this[int index] {
			get {
				if (index < 0 || index >= this.Count)
					throw new ArgumentOutOfRangeException(nameof(index));
				return this.pointers[index];
			}
		}
		#endregion
	}
	#endregion


	#region cached storage classes
	/// <summary>
	/// The interface for any cached storage, including actual ones and referenced one
	/// </summary>
	public interface ICachedStorage : IStorage
	{
		#region new methods
		/// <summary>
		/// When implemented by a derived class, get the cache sizes of each level as a <see cref="IReadOnlyList{T}"/> of <see cref="ulong"/>
		/// </summary>
		IReadOnlyList<ulong> CacheSizes { get; }

		/// <summary>
		/// Encapsulates a method that copies <paramref name="source"/> to <paramref name="destination"/> where both are <see cref="PointerSegment"/>.
		/// </summary>
		/// <param name="source">The source <see cref="PointerSegment"/> to copy from</param>
		/// <param name="destination">The destination <see cref="PointerSegment"/> to copy to</param>
		public delegate void CopyDelegate(PointerSegment source, PointerSegment destination);

		/// <summary>
		/// When implemented by a derived class, retrieve some part of the data delimited by <paramref name="lengthInBytes"/> and <paramref name="totalOffsetInBytes"/> via promoting them to the highest caching level.
		/// </summary>
		/// <param name="totalOffsetInBytes">The total offset (in bytes) in the lowest caching level (the cache level where all the data preserves)</param>
		/// <param name="lengthInBytes">The length to retrieve (in bytes), default 0 means retrieve as much as possible</param>
		/// <param name="copy">The <see cref="CopyDelegate"/> used to copy data between caching levels. The default null value can be replaced by the <see cref="MEM.SelectImplementation{T}(Storage{T}, Storage{T})"/>.<see cref="MEM.MemoryCopy(PointerSegment, PointerSegment)">MemoryCopy</see>.</param>
		/// <returns>The <see cref="PointerSegment"/> of the highest caching level containing the required data</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="totalOffsetInBytes"/> or <paramref name="totalOffsetInBytes"/> is out of the boundary, or <paramref name="lengthInBytes"/> is larger than the size of the highest caching level</exception>
		/// <remarks>
		/// Some caching strategies and algorithms (such as the ones utilized by modern computers) shall be used to improve performance.<br/>
		/// It is not necessary to write the data in the higher caching level back to the lower one immediately while it is necessary if some new data are retrieved.
		/// </remarks>
		PointerSegment Retrieve(ulong totalOffsetInBytes, ulong lengthInBytes = 0, CopyDelegate? copy = null);

		/// <summary>
		/// When implemented by a derived class, update all the data in the lowest caching level by causing all caching levels to fall back to the lower ones.
		/// </summary>
		void Flush();
		#endregion
	}

	/// <summary>
	/// An abstract class which represents a storage of several contiguous memory blocks on different memory locations with variable sizes purposed to cache memories of higher performance. Inherits <see cref="ActualStorage{T}"/>.
	/// </summary>
	/// <typeparam name="T">any unmanaged data type</typeparam>
	public abstract class CachedStorage<T> : ActualStorage<T>, ICachedStorage where T : unmanaged
	{
		#region basic
		/// <summary>
		/// The description of the storage locations of this storage class as a <see cref="CombinationOfLocations"/>
		/// </summary>
		public override CombinationOfLocations LocationDescription { get; }

		/// <summary>
		/// The cache sizes of each level as a <see cref="IReadOnlyList{T}"/> of <see cref="ulong"/>
		/// </summary>
		public virtual IReadOnlyList<ulong> CacheSizes { get; }

		/// <summary>
		/// Create (without allocation) a <see cref="CachedStorage{T}"/> of given <see cref="StorageLocation"/>s and <see cref="ulong"/>s as priorities and total length (<see cref="Storage{T}.Length"/>) in <typeparamref name="T"/>
		/// </summary>
		/// <param name="locations">The <see cref="ReadOnlySpan{T}"/> of <see cref="StorageLocation"/> to represent the caching levels from higher-performance ones to lower ones. It cannot contain any duplicate values or has size less than 2.</param>
		/// <param name="maxLengths">The <see cref="ReadOnlySpan{T}"/> of <see cref="ulong"/> to represent the maximum size of each caching levels. The last value is the actual length in <typeparamref name="T"/> It must be of same size as <paramref name="locations"/>.</param>
		/// <exception cref="ArgumentNullException">If the sizes of <paramref name="locations"/> or <paramref name="maxLengths"/> is 0</exception>
		/// <exception cref="ArgumentException">If <paramref name="locations"/> is of wrong size or has duplicate value(s) or is of wrong size; or if <paramref name="maxLengths"/> is of wrong size or has non-increase cache size</exception>
		/// <exception cref="ArgumentOutOfRangeException">If any length in <paramref name="maxLengths"/> is 0</exception>
		protected CachedStorage(ReadOnlySpan<StorageLocation> locations, ReadOnlySpan<ulong> maxLengths) : base(maxLengths[^1])
		{
			if (locations.Length <= 1)
				throw new ArgumentException(Parameter.WrongSize, nameof(locations));
			if (maxLengths.Length <= 1)
				throw new ArgumentException(Parameter.WrongSize, nameof(maxLengths));
			if (maxLengths.Length != locations.Length)
				throw new ArgumentException(Parameter.NotSameSize);
			if (!locations.ElementsUnique())
				throw new ArgumentException(Parameter.DuplicateValue, nameof(locations));
			if (maxLengths.Any(l => l == 0))
				throw new ArgumentOutOfRangeException(nameof(maxLengths), Parameter.MustPositive);
			// check ratios
			for (int i = 1; i < locations.Length; i++)
			{
				if (maxLengths[i] <= maxLengths[i - 1])
					throw new ArgumentException(Parameter.InvalidValue, nameof(maxLengths));
				else if (maxLengths[i] / maxLengths[i - 1] < 10)
					Helpers.Log.Write(Other.CacheSizeRatioSmall, level: Helpers.LogLevel.Warning);
			}
			this.LocationDescription = new CombinationOfLocations(CombinationType.Cached, locations);
			this.CacheSizes = maxLengths.ToArray();
		}
		#endregion

		#region override
		/// <summary>
		/// The function that actually dispose this storage, override <see cref="Storage{T}.Dispose(bool)"/>
		/// </summary>
		/// <param name="disposeManaged">dispose managed resources or not</param>
		protected override void Dispose(bool disposeManaged)
		{
			for (int i = 0; i < this.CacheSizes.Count; i++)
			{
				var ptr = this.GetCacheLevel(i);
				if (ptr.IsValid())
					MEM.SelectImplementation(ptr.Location).Free(ptr, disposeManaged);
			}
		}

		/// <summary>
		/// The number of <see cref="PointerSegment"/>(s) of this <see cref="Storage{T}"/>, always return 1
		/// </summary>
		public override int Count => 1;

		/// <summary>
		/// Indexer of the <see cref="PointerSegment"/>(s) of this <see cref="Storage{T}"/> (in presenting order)
		/// </summary>
		/// <param name="index">the element index, must be 0</param>
		/// <returns>the <see cref="PointerSegment"/> at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range</exception>
		/// <remarks>You can <b>only</b> modify the data of the result of this indexer <b>right after</b> calling <see cref="Flush"/>. Otherwise, it may cause unexpected results.</remarks>
		public override PointerSegment this[int index] {
			get {
				if (index != 0)
					throw new ArgumentOutOfRangeException(nameof(index));
				return this.GetCacheLevel(^1);
			}
		}
		#endregion

		#region new methods
		/// <summary>
		/// Get the whole caching level at <paramref name="index"/> as a <see cref="PointerSegment"/>
		/// </summary>
		/// <param name="index">The <see cref="Index"/> to indicate the level</param>
		/// <returns>The whole caching level at <paramref name="index"/> as a <see cref="PointerSegment"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range</exception>
		protected abstract PointerSegment GetCacheLevel(Index index);

		/// <summary>
		/// When implemented by a derived class, retrieve some part of the data delimited by <paramref name="lengthInBytes"/> and <paramref name="totalOffsetInBytes"/> via promoting them to the highest caching level.
		/// </summary>
		/// <param name="totalOffsetInBytes">The total offset (in bytes) in the lowest caching level (the cache level where all the data preserves)</param>
		/// <param name="lengthInBytes">The length to retrieve (in bytes), default 0 means retrieve as much as possible</param>
		/// <param name="copy">The <see cref="ICachedStorage.CopyDelegate"/> used to copy data between caching levels. The default null value can be replaced by the <see cref="MEM.SelectImplementation{T}(Storage{T}, Storage{T})"/>.<see cref="MEM.MemoryCopy(PointerSegment, PointerSegment)">MemoryCopy</see>.</param>
		/// <returns>The <see cref="PointerSegment"/> of the highest caching level containing the required data</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="totalOffsetInBytes"/> or <paramref name="totalOffsetInBytes"/> is out of the boundary, or <paramref name="lengthInBytes"/> is larger than the size of the highest caching level</exception>
		/// <remarks>
		/// Some caching strategies and algorithms (such as the ones utilized by modern computers) shall be used to improve performance.<br/>
		/// It is not necessary to write the data in the higher caching level back to the lower one immediately while it is necessary if some new data are retrieved.
		/// </remarks>
		public abstract PointerSegment Retrieve(ulong totalOffsetInBytes, ulong lengthInBytes = 0, ICachedStorage.CopyDelegate? copy = null);

		/// <summary>
		/// When implemented by a derived class, update all the data in the lowest caching level by causing all caching levels to fall back to the lower ones.
		/// </summary>
		public abstract void Flush();
		#endregion
	}

	/// <summary>
	/// The storage class that references to a <see cref="CachedStorage{T}"/>, implements <see cref="ReferenceStorage{T}"/>
	/// </summary>
	/// <typeparam name="T">any unmanaged data type</typeparam>
	public class CachedReferenceStorage<T> : ReferenceStorage<T>, IReferenceStorage, ICachedStorage where T : unmanaged
	{
		#region basic
		/// <summary>
		/// The cache sizes of each level as a <see cref="IReadOnlyList{T}"/> of <see cref="ulong"/>
		/// </summary>
		public virtual IReadOnlyList<ulong> CacheSizes => (this.Reference as ICachedStorage)?.CacheSizes ?? Array.Empty<ulong>();

		/// <summary>
		/// Get the number of <see cref="PointerSegment"/>(s) of this <see cref="Storage{T}"/> 
		/// </summary>
		public override int Count => this.Reference is null ? 0 : 1;

		/// <summary>
		/// Get the description of the storage locations of this <see cref="Storage{T}"/> class as a <see cref="CombinationOfLocations"/>
		/// </summary>
		public override CombinationOfLocations LocationDescription => this.Reference?.LocationDescription ?? default;

		/// <summary>
		/// Create a <see cref="CachedReferenceStorage{T}"/> with given reference <paramref name="storage"/> and <paramref name="offset"/> to it
		/// </summary>
		/// <param name="storage">the <see cref="CachedStorage{T}"/> to be referenced</param>
		/// <param name="offset">the total offset in <typeparamref name="T"/> as a <see cref="long"/></param>
		/// <param name="newLength">the new presenting length in <typeparamref name="T"/>, default 0 means automatically calculate by <paramref name="storage"/> and <paramref name="offset"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="storage"/> or its reference is null</exception>
		/// <exception cref="ArgumentException">If <paramref name="storage"/> is not a <see cref="CachedStorage{T}"/></exception>
		public CachedReferenceStorage(IStorage? storage, long offset = 0, ulong newLength = 0) : base(storage, offset, newLength)
		{
			if (this.Reference is null)
				return;
			// check 
			if (this.Reference is not CachedStorage<T> && !this.Reference.GetType().MakeGenericType(typeof(T)).IsAssignableTo(typeof(CachedStorage<T>)))
				throw new ArgumentException(Parameter.UnexpectedType, nameof(storage));
		}
		#endregion

		#region override
		/// <summary>
		/// Indexer of the <see cref="PointerSegment"/>(s) of this <see cref="CachedReferenceStorage{T}"/> (in presenting order)
		/// </summary>
		/// <param name="index">the element index</param>
		/// <returns>the <see cref="PointerSegment"/> at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of the range</exception>
		/// <exception cref="InvalidOperationException">If the referenced storage of this <see cref="CachedReferenceStorage{T}"/> is null</exception>
		/// <remarks>You can <b>only</b> modify the data of the result of this indexer <b>right after</b> calling <see cref="Flush"/>. Otherwise, it may cause unexpected results.</remarks>
		public override PointerSegment this[int index] {
			get {
				if (index != 0)
					throw new ArgumentOutOfRangeException(nameof(index));
				if (this.Reference is null)
					throw new InvalidOperationException();
				return this.Reference[0].MoveBy((long)this.TotalOffsetInBytes, this.LengthInBytes);
			}
		}

		/// <summary>
		/// Make a <see cref="ReferenceStorage{T}"/> with the starting pointer moving <paramref name="offset"/> and <see cref="Storage{T}.Length"/> changing to <paramref name="newLength"/>.
		/// </summary>
		/// <param name="offset">The offset in <typeparamref name="T"/> to the starting pointer of this <see cref="Storage{T}"/> as a <see cref="long"/></param>
		/// <param name="newLength">The new length in <typeparamref name="T"/> as a <see cref="ulong"/>, default 0 means automatically calculate from <paramref name="offset"/></param>
		/// <returns>A <see cref="CachedReferenceStorage{T}"/> of this one</returns>
		public override CachedReferenceStorage<T> MakeReference(long offset = 0, ulong newLength = 0)
		{
			return new CachedReferenceStorage<T>(this, offset, newLength);
		}

		/// <summary>
		/// Convert this <see cref="CachedReferenceStorage{T}"/> to another one with different data type <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">the output data type</typeparam>
		/// <returns>a referenced <see cref="CachedReferenceStorage{TOut}"/></returns>
		/// <exception cref="InvalidCastException">if <see cref="IStorage.LengthInBytes"/> cannot be divided by <see cref="Storage{TOut}.SizeOfT"/></exception>
		public override CachedReferenceStorage<TOut> As<TOut>()
		{
			if (this.Reference is null)
				return new CachedReferenceStorage<TOut>(null, 0, 0);
			long offset = CheckCast<TOut>((long)this.TotalOffsetInBytes, sizeInBytes: true);
			ulong length = CheckCast<TOut>(this.Reference.LengthInBytes - this.TotalOffsetInBytes, sizeInBytes: true);
			return new CachedReferenceStorage<TOut>(this.Reference, offset, length);
		}

		/// <summary>
		/// Retrieve some part of the data delimited by <paramref name="lengthInBytes"/> and <paramref name="totalOffsetInBytes"/> via promoting them to the highest caching level.
		/// </summary>
		/// <param name="totalOffsetInBytes">The total offset (in bytes) in the lowest caching level (the cache level where all the data preserves)</param>
		/// <param name="lengthInBytes">The length to retrieve (in bytes), default 0 means retrieve as much as possible</param>
		/// <param name="copy">The <see cref="ICachedStorage.CopyDelegate"/> used to copy data between caching levels. The default null value can be replaced by the <see cref="MEM.SelectImplementation{T}(Storage{T}, Storage{T})"/>.<see cref="MEM.MemoryCopy(PointerSegment, PointerSegment)">MemoryCopy</see>.</param>
		/// <returns>The <see cref="PointerSegment"/> of the highest caching level containing the required data</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="totalOffsetInBytes"/> or <paramref name="totalOffsetInBytes"/> is out of the boundary, or <paramref name="lengthInBytes"/> is larger than the size of the highest caching level</exception>
		/// <remarks>This method utilizes the <see cref="ICachedStorage.Retrieve(ulong, ulong, ICachedStorage.CopyDelegate?)"/> of <see cref="ReferenceStorage{T}.Reference"/></remarks>
		public PointerSegment Retrieve(ulong totalOffsetInBytes, ulong lengthInBytes = 0, ICachedStorage.CopyDelegate? copy = null)
		{
			if (this.Reference is ICachedStorage c)
				return c.Retrieve(this.TotalOffsetInBytes + totalOffsetInBytes, lengthInBytes, copy);
			else
				return default;
		}

		/// <summary>
		/// Update all the data in the lowest caching level by causing all caching levels to fall back to the lower ones.
		/// </summary>
		/// <remarks>This method utilizes the <see cref="ICachedStorage.Flush()"/> of <see cref="ReferenceStorage{T}.Reference"/></remarks>
		public void Flush()
		{
			(this.Reference as ICachedStorage)?.Flush();
		}

		/// <summary>
		/// Determines whether the specified object is equal to the current object.
		/// </summary>
		/// <param name="obj">another object</param>
		/// <returns>this equals to <paramref name="obj"/> or not</returns>
		public override bool Equals(Storage<T>? obj)
		{
			if (obj is not null && obj is CachedReferenceStorage<T> @ref)
			{
				return this.Reference == @ref.Reference && this.TotalOffsetInBytes == @ref.TotalOffsetInBytes && this.Length == @ref.Length;
			}
			return false;
		}

		/// <summary>
		/// Get the hash code of this <see cref="ReferenceStorage{T}"/>
		/// </summary>
		/// <returns>the hash code</returns>
		public override int GetHashCode() => HashCode.Combine(this.Reference, this.TotalOffsetInBytes, this.Length);
		#endregion
	}
	#endregion


	#region factory
	/// <summary>
	/// The storage factory for creating concrete storage classes. This is a simple factory pattern.
	/// </summary>
	/// <typeparam name="T">any unmanaged data type</typeparam>
	public static class StorageFactory<T> where T : unmanaged
	{
		/// <summary>
		/// Encapsulates a method that allocates and creates a new <see cref="Storage{T}"/> with given <paramref name="locations"/> and <paramref name="lengths"/>
		/// </summary>
		/// <param name="locations">The given <see cref="CombinationOfLocations"/> indicating the locations</param>
		/// <param name="lengths">The given <see cref="Span{T}"/> of <see cref="ulong"/> indicating the length in <typeparamref name="T"/> of each location</param>
		/// <returns>The created new <see cref="Storage{T}"/></returns>
		/// <remarks>Independent checks for parameters are not necessary</remarks>
		public delegate Storage<T> CreateDelegate(Span<StorageLocation> locations, Span<ulong> lengths);

		private static readonly Dictionary<CombinationType, CreateDelegate> cache_create = new Dictionary<CombinationType, CreateDelegate>
		{
			[CombinationType.PureOrMixed] = DefaultCreatePureOrMixed,
			[CombinationType.Cached] = DefaultCreateCached,
		};

		private static Storage<T> DefaultCreatePureOrMixed(Span<StorageLocation> locations, Span<ulong> lengths)
		{
			if (locations.Length == 1)
				return new PureStorage<T>(locations[0], lengths[0]);
			else
				return new MixedStorage<T>(locations, lengths);
		}

		private static Storage<T> DefaultCreateCached(Span<StorageLocation> locations, Span<ulong> lengths)
		{

		}

		/// <summary>
		/// Set the creation method for a given <see cref="CombinationType"/>
		/// </summary>
		/// <param name="combinationType">The given <see cref="CombinationType"/> to set the creation method</param>
		/// <param name="createDelegate">The creation method as a <see cref="CreateDelegate"/></param>
		public static void SetCreateMethod(CombinationType combinationType, CreateDelegate createDelegate)
		{
			cache_create[combinationType] = createDelegate;
		}

		/// <summary>
		/// Allocate and create a new <see cref="Storage{T}"/> with given <paramref name="locations"/> and <paramref name="lengths"/>
		/// </summary>
		/// <param name="type">The <see cref="CombinationType"/> used to identify which creation method to use</param>
		/// <param name="locations">The given <see cref="CombinationOfLocations"/> indicating the locations</param>
		/// <param name="lengths">The given <see cref="Span{T}"/> of <see cref="ulong"/> indicating the length in <typeparamref name="T"/> of each location</param>
		/// <returns>The created new <see cref="Storage{T}"/></returns>
		/// <exception cref="InvalidOperationException">If the creation method of <paramref name="type"/> is neither default indicated nor manually indicated by <see cref="SetCreateMethod(CombinationType, CreateDelegate)"/></exception>
		public static Storage<T> Create(CombinationType type, Span<StorageLocation> locations, Span<ulong> lengths)
		{
			if (!cache_create.ContainsKey(type))
				throw new InvalidOperationException();

			return cache_create[type].Invoke(locations, lengths);
		}
	}
	#endregion
}
