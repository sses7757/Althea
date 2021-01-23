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

		bool IPointer.IsValidLocation(StorageLocation location) => location.Location.GetClassification() == LocationTypeExtension.ClassMemory;

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

		bool IPointer.IsValidLocation(StorageLocation location) => location.Location.GetClassification() == LocationTypeExtension.ClassStream;

		IReadOnlyDictionary<string, string> IMainPropertyFormat.StringProperties => new Dictionary<string, string>
		{ 
			["length"] = this.NativeStream.Length.ToString(),
			["position"] = this.NativeStream.Position.ToString(),
		};
		#endregion
	}
	#endregion


	#region concrete storage classes
	/// <summary>
	/// The abstract storage class as a base class for all non-referenced <see cref="Storage{T}"/> classes
	/// </summary>
	/// <typeparam name="T">any unmanaged data type</typeparam>
	public abstract class ActualStorage<T> : Storage<T> where T : unmanaged
	{
		#region memory
		/// <summary>
		/// The total length of the presenting array in <typeparamref name="T"/> (rather than bytes), override <see cref="Storage{T}.Length"/>
		/// </summary>
		public override ulong Length { get; }

		/// <summary>
		/// Create an <see cref="ActualStorage{T}"/> with given length of presenting array
		/// </summary>
		/// <param name="length">the length of presenting array <typeparamref name="T"/></param>
		protected ActualStorage(ulong length)
		{
			if (length == 0)
				throw new ArgumentOutOfRangeException(nameof(length), Parameter.MustPositive);
			this.Length = length;
		}

		/// <summary>
		/// The finalizer of <see cref="ActualStorage{T}"/>
		/// </summary>
		~ActualStorage()
		{
			this.Dispose(false);
		}

		/// <summary>
		/// The function that actually dispose this storage, override <see cref="Storage{T}.Dispose(bool)"/>
		/// </summary>
		/// <param name="disposeManaged">dispose managed resources or not</param>
		protected override void Dispose(bool disposeManaged)
		{
			for (int i = 0; i < this.Count; i++)
			{
				var ptr = this[i];
				MEM.SelectImplementation(ptr.Location).Free(ptr, disposeManaged);
			}
		}

		/// <summary>
		/// Allocate a <see cref="PointerSegment"/> of given <see cref="Storage{T}.Length"/> on given <see cref="StorageLocation"/> 
		/// </summary>
		/// <param name="location">a <see cref="StorageLocation"/> to represent the memory location</param>
		/// <param name="length">the length of contiguous memory block in <typeparamref name="T"/></param>
		protected static PointerSegment Allocate(StorageLocation location, ulong length)
		{
			return MEM.SelectImplementation(location).Allocate<T>(location, length);
		}
		#endregion

		#region override
		/// <summary>
		/// Convert this <see cref="ActualStorage{T}"/> to another one with different data type <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">the output data type</typeparam>
		/// <returns>a <see cref="ReferenceStorage{TOut}"/></returns>
		/// <exception cref="InvalidCastException">if <see cref="IStorage.LengthInBytes"/> cannot be divided by <see cref="Storage{TOut}.SizeOfT"/></exception>
		public override ReferenceStorage<TOut> As<TOut>()
		{
			if (this.LengthInBytes % Storage<TOut>.SizeOfT != 0)
				throw new InvalidCastException(Other.CannotDivide);
			ulong newLength = this.LengthInBytes / Storage<TOut>.SizeOfT;
			return new ReferenceStorage<TOut>(this, newLength: newLength);
		}

		/// <summary>
		/// Determines whether the specified object is equal to the current object.
		/// </summary>
		/// <param name="obj">another object</param>
		/// <returns>this equals to <paramref name="obj"/> or not</returns>
		public override bool Equals(Storage<T>? obj)
		{
			if (obj is not null && obj is ActualStorage<T> another)
			{
				if (this.Count != another.Count)
					return false;
				for (int i = 0; i < this.Count; i++)
				{
					if (this[i] != another[i])
						return false;
				}
				return true;
			}
			return false;
		}

		/// <summary>
		/// Get the hash code of this <see cref="PureStorage{T}"/>
		/// </summary>
		/// <returns>the hash code</returns>
		public override int GetHashCode() => this.HashCodeOfArray();
		#endregion
	}

	/// <summary>
	/// Represents a storage of a contiguous memory block on a certain memory location, inherits <see cref="Storage{T}"/>
	/// </summary>
	/// <typeparam name="T">any unmanaged data type</typeparam>
	public class PureStorage<T> : ActualStorage<T> where T : unmanaged
	{
		#region basic
		private readonly PointerSegment pointer;

		/// <summary>
		/// The description of the storage locations of this storage class as a <see cref="CombinationOfLocations"/>
		/// </summary>
		public override CombinationOfLocations LocationDescription => this.pointer.Location;

		/// <summary>
		/// <b>Allocate</b> and create a <see cref="PureStorage{T}"/> of given <see cref="Storage{T}.Length"/> on given <see cref="StorageLocation"/> 
		/// </summary>
		/// <param name="location">a <see cref="StorageLocation"/> to represent the memory location</param>
		/// <param name="length">the length of contiguous memory block in <typeparamref name="T"/></param>
		public PureStorage(StorageLocation location, ulong length) : base(length)
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
		public override PointerSegment this[int index] {
			get {
				if (index < 0 || index >= 1)
					throw new ArgumentOutOfRangeException(nameof(index));
				return pointer;
			}
		}
		#endregion
	}

	/// <summary>
	/// Represents a storage of several contiguous memory blocks on different memory locations with fixed sizes, inherits <see cref="Storage{T}"/>
	/// </summary>
	/// <typeparam name="T">any unmanaged data type</typeparam>
	public class MixedStorage<T> : ActualStorage<T> where T : unmanaged
	{
		#region basic
		private readonly PointerSegment[] pointers;

		/// <summary>
		/// The description of the storage locations of this storage class as a <see cref="CombinationOfLocations"/>
		/// </summary>
		public override CombinationOfLocations LocationDescription {
			get {
				Span<StorageLocation> span = stackalloc StorageLocation[this.Count];
				this.CopyTo(span, p => p.Location);
				return new CombinationOfLocations(CombinationType.PureOrMixed, span);
			}
		}

		/// <summary>
		/// <b>Allocate</b> and create a <see cref="MixedStorage{T}"/> of given lengths on given <see cref="StorageLocation"/>s
		/// </summary>
		/// <param name="param">the <see cref="IReadOnlyList{T}"/> of given lengths and <see cref="StorageLocation"/>s</param>
		/// <param name="allowSameLocation">allow same <see cref="StorageLocation"/>s in <paramref name="param"/> or not</param>
		public MixedStorage(IReadOnlyList<(StorageLocation location, ulong length)> param, bool allowSameLocation = true) : base(param.Sum(p => p.length))
		{
			if (param.Count <= 1)
				throw new ArgumentOutOfRangeException(nameof(param), Parameter.WrongSize);
			this.pointers = new PointerSegment[param.Count];
			for (int i = 0; i < param.Count; i++)
			{
				var (location, length) = param[i];
				if (!allowSameLocation && pointers.Contains(location, selector: p => p.Location))
					throw new ArgumentOutOfRangeException(nameof(param), Parameter.DuplicateValue);
				this.pointers[i] = Allocate(location, length);
			}
		}

		/// <summary>
		/// <b>Allocate</b> and create a <see cref="MixedStorage{T}"/> of given lengths on given <see cref="StorageLocation"/>s
		/// </summary>
		/// <param name="param">the <see cref="Array"/> of given <see cref="Storage{T}.Length"/>s on given <see cref="StorageLocation"/>s</param>
		public MixedStorage(params (StorageLocation location, ulong length)[] param) : this(param as IReadOnlyList<(StorageLocation location, ulong length)>) { }

		/// <summary>
		/// <b>Allocate</b> and create a <see cref="MixedStorage{T}"/> of given lengths on given <see cref="StorageLocation"/>s
		/// </summary>
		/// <param name="locations">the <see cref="IEnumerable{T}"/> of given <see cref="StorageLocation"/>s</param>
		/// <param name="lengths">the <see cref="IEnumerable{T}"/> of given lengths</param>
		/// <param name="allowSameLocation">allow same <see cref="StorageLocation"/>s in <paramref name="locations"/> or not</param>
		public MixedStorage(IReadOnlyList<StorageLocation> locations, IReadOnlyList<ulong> lengths, bool allowSameLocation = true) : this(locations.Zip(lengths), allowSameLocation) { }
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
		public override PointerSegment this[int index] {
			get {
				if (index < 0 || index >= this.Count)
					throw new ArgumentOutOfRangeException(nameof(index));
				return this.pointers[index];
			}
		}
		#endregion
	}

	/// <summary>
	/// Represents a storage of several contiguous memory blocks on different memory locations with variable sizes purposed to cache memories of higher performance, inherits <see cref="Storage{T}"/>
	/// </summary>
	/// <typeparam name="T">any unmanaged data type</typeparam>
	public class CachedStorage<T> : ActualStorage<T> where T : unmanaged
	{
		#region basic
		private readonly PointerSegment[] pointers;

		/// <summary>
		/// The description of the storage locations of this storage class as a <see cref="CombinationOfLocations"/>
		/// </summary>
		public override CombinationOfLocations LocationDescription {
			get {
				Span<StorageLocation> span = stackalloc StorageLocation[this.Count];
				this.CopyTo(span, p => p.Location);
				return new CombinationOfLocations(CombinationType.Cached, span);
			}
		}

		/// <summary>
		/// <b>Allocate</b> and create a <see cref="CachedStorage{T}"/> of given <see cref="StorageLocation"/>s and <see cref="ulong"/>s as priorities and total length (<see cref="Storage{T}.Length"/>) in <typeparamref name="T"/>
		/// </summary>
		/// <param name="priorities">the <see cref="IEnumerable{T}"/> of <see cref="StorageLocation"/>s and <see cref="ulong"/>s to represent the priorities from higher-performance memories to lower ones (cannot contain <see cref="LocationType.Uri"/> or any duplicate locations)</param>
		/// <param name="totalLength">the desired total length (in <typeparamref name="T"/>) of presenting array</param>
		/// <exception cref="ArgumentException">if <paramref name="priorities"/> has unexpected value(s) or is of wrong size</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="totalLength"/> or <paramref name="cacheUri"/> has unexpected value(s)</exception>
		public CachedStorage(IEnumerable<(StorageLocation location, ulong maxLengthInBytes)> priorities, ulong totalLength) : base(totalLength)
		{
			var temp = new List<PointerSegment>();
			foreach (var (location, maxLengthInBytes) in priorities)
			{
				if (location.Location == LocationType.Uri)
					throw new ArgumentException(Parameter.UnexpectedValue, nameof(priorities));
				if (temp.Contains(location, selector: p => p.Location))
					throw new ArgumentException(Parameter.DuplicateValue, nameof(priorities));
				ulong length = 0; // TODO: adapter get available length on 'location'
				if (maxLengthInBytes != 0 && maxLengthInBytes < length)
				{
					length = maxLengthInBytes;
				}
				// do not allocate here
				temp.Add(new StoragePointer(location, default(IntPtr), length));
			}
			if (temp.Count <= 1)
				throw new ArgumentException(Parameter.WrongSize, nameof(priorities));
			ulong allowedTotalLength = temp.Sum(p => p.LengthInBytes);
			if (allowedTotalLength <= totalLength && cacheUri is null)
				throw new ArgumentOutOfRangeException(nameof(totalLength));
			// allocate here
			// TODO: adapter allocate
		}
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
}
