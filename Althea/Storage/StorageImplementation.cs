using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

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
		/// Get the raw pointer of this <see cref="IMemoryPointer"/> as a <see cref="IntPtr"/>
		/// </summary>
		IntPtr Pointer { get; }

		bool ICheckValid.IsValid() => this.Pointer != default && this.LengthInBytes != 0;

		bool IPointer.IsValidLocation(StorageLocation location) => location.Location.GetClassification() == LocationTypeExtension.ClassMemory;

		bool IPointer.CanRead => true;

		bool IPointer.CanWrite => true;

		bool IPointer.CanReadOffset => true;

		bool IPointer.CanWriteOffset => true;

		bool IPointer.CanResize => false;

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
		unsafe T* UnmangedPointer<T>(long offset = 0) where T : unmanaged => (T*)this.Pointer.ToPointer() + offset;

		/// <summary>
		/// Get the native pointer of this <see cref="IMemoryPointer"/> with a given offset as a <see cref="void"/>*
		/// </summary>
		/// <param name="offset">The offset in bytes to the <see cref="Pointer"/> of this <see cref="IMemoryPointer"/></param>
		/// <returns>The native pointer with given offset as a <see cref="void"/>*</returns>
		unsafe void* NativePointer(long offset = 0) => (byte*)this.Pointer.ToPointer() + offset;

		/// <summary>
		/// Get the <see cref="Span{T}"/> representation of this <see cref="IMemoryPointer"/> with given offset and length
		/// </summary>
		/// <typeparam name="T">any unmanaged data type</typeparam>
		/// <param name="offset">The offset in <typeparamref name="T"/> to the <see cref="Pointer"/> of this <see cref="IMemoryPointer"/> before converting to <see cref="Span{T}"/></param>
		/// <param name="length">The presenting length in <typeparamref name="T"/> of this <see cref="IMemoryPointer"/> before converting to <see cref="Span{T}"/></param>
		/// <returns>The <see cref="Span{T}"/> representation of this <see cref="IMemoryPointer"/></returns>
		unsafe Span<T> AsSpan<T>(long offset = 0, int length = 0) where T : unmanaged => new Span<T>(this.UnmangedPointer<T>(offset), length == 0 ? checked((int)this.LengthInBytes / sizeof(T)) : length);

		/// <summary>
		/// Get the <see cref="Span{T}"/> representation of this <see cref="IMemoryPointer"/> with given offset and length
		/// </summary>
		/// <typeparam name="T">any unmanaged data type</typeparam>
		/// <param name="pointerSegment">Use the given <see cref="PointerSegment"/> to obtain offset and length</param>
		/// <returns>The <see cref="Span{T}"/> representation of this <see cref="IMemoryPointer"/></returns>
		Span<T> AsSpan<T>(PointerSegment pointerSegment) where T : unmanaged => this.AsSpan<T>(checked((long)(pointerSegment.OffsetInBytes / Storage<T>.SizeOfT)), checked((int)(pointerSegment.LengthInBytes / Storage<T>.SizeOfT)));
		#endregion
	}

	/// <summary>
	/// The "interface" for an immutable pointer at any possible stream storage which can be described by a <see cref="Stream"/>
	/// </summary>
	public abstract class AbstractStreamPointer : IPointer, IDisposable
	{
		#region basic
		/// <summary>
		/// Get the raw stream of this <see cref="AbstractStreamPointer"/> used for reading as a <see cref="Stream"/>. Shall be null if this <see cref="AbstractStreamPointer"/> does not support reading.
		/// </summary>
		protected abstract Stream ReadStream { get; }

		/// <summary>
		/// When implemented by sub-classes, this method shall perform the operations need when a <see cref="ReadStream"/> just stopped using. The default implementation does nothing.
		/// </summary>
		protected virtual void OnReadFinish() { }

		/// <summary>
		/// Get the raw stream of this <see cref="AbstractStreamPointer"/> used for writing as a <see cref="Stream"/>. Shall be null if this <see cref="AbstractStreamPointer"/> does not support writing.
		/// </summary>
		protected abstract Stream WriteStream { get; }

		/// <summary>
		/// When implemented by sub-classes, this method shall perform the operations need when a <see cref="WriteStream"/> just stopped using. The default implementation does nothing.
		/// </summary>
		protected virtual void OnWriteFinish() { }

		/// <summary>
		/// The basic description of this <see cref="AbstractStreamPointer"/> as a <see cref="string"/>, such as <see cref="Uri.ToString"/>
		/// </summary>
		protected abstract string Description { get; }

		private bool disposed;

		/// <summary>
		/// The method to be implemented to actually dispose the resources held by this <see cref="AbstractStreamPointer"/>. The default implementation only disposes <see cref="ReadStream"/> and <see cref="WriteStream"/>.
		/// </summary>
		/// <param name="disposing">Dispose managed resources or not</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
					// do nothing
				}
				this.ReadStream?.Dispose();
				this.WriteStream?.Dispose();
				disposed = true;
			}
		}

		/// <summary>
		/// Dispose resources held by this class
		/// </summary>
		public void Dispose()
		{
			this.Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
		#endregion

		#region implemented interfaces
		/// <summary>
		/// The original length of this pointer's underlying storage in bytes
		/// </summary>
		public virtual ulong LengthInBytes => (ulong)(this.ReadStream ?? this.WriteStream).Length;

		/// <summary>
		/// Check whether this object is a valid one or not
		/// </summary>
		/// <returns>The validness of this object</returns>
		public virtual bool IsValid() => (this.ReadStream is not null || this.WriteStream is not null) && this.LengthInBytes != 0;

		/// <summary>
		/// <b>Statically</b> check whether given <paramref name="location"/> is a supported one for this pointer
		/// </summary>
		/// <param name="location">The given <see cref="StorageLocation"/> to be checked</param>
		/// <returns>Whether given <paramref name="location"/> is supported or not</returns>
		public virtual bool IsValidLocation(StorageLocation location) => location.Location.GetClassification() == LocationTypeExtension.ClassStream;

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether this pointer can be read or not
		/// </summary>
		public virtual bool CanRead => this.ReadStream.CanRead;

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether this pointer can be written or not
		/// </summary>
		public virtual bool CanWrite => this.WriteStream.CanWrite;

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether this pointer can be read with offset or not
		/// </summary>
		public virtual bool CanReadOffset => this.ReadStream.CanSeek && this.ReadStream.CanRead;

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether this pointer can be written with offset or not
		/// </summary>
		public virtual bool CanWriteOffset => this.WriteStream.CanSeek && this.WriteStream.CanWrite;

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether this pointer can be resized in-place or not
		/// </summary>
		public virtual bool CanResize => this.CanWriteOffset;

		string IMainPropertyFormat.StringMain => this.Description;

		IReadOnlyDictionary<string, string> IMainPropertyFormat.StringProperties => new Dictionary<string, string>
		{ 
			["length"] = this.LengthInBytes.ToString(),
		};
		#endregion

		#region new default implementations
		/// <summary>
		/// Get the default buffer size in bytes which is divisible by the size of <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">any unmanaged data type</typeparam>
		/// <returns>The default buffer size in bytes divisible by the size of <typeparamref name="T"/></returns>
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		protected static unsafe int BufferSizeInBytes<T>() where T : unmanaged => (1 << 16) / sizeof(T) * sizeof(T);

		/// <summary>
		/// Set the values of this <see cref="AbstractStreamPointer"/> <typeparamref name="T"/> by <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">any unmanaged data type</typeparam>
		/// <param name="offset">The offset in <b><typeparamref name="T"/></b> rather than bytes</param>
		/// <param name="length">The length to set in <b><typeparamref name="T"/></b> rather than bytes</param>
		/// <param name="value">The value to set</param>
		/// <exception cref="ArgumentException">If the <see cref="ReadStream"/> is too short</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> or <paramref name="length"/> exceeds the boundary</exception>
		/// <exception cref="IOException">If an I/O error occurs</exception>
		/// <exception cref="NotSupportedException">If the <see cref="WriteStream"/> does not support seeking or writing</exception>
		/// <exception cref="ObjectDisposedException">If the <see cref="WriteStream"/> is already closed</exception>
		/// <remarks>When overridden by derived classes, <see cref="OnWriteFinish"/> shall be invoked at last.</remarks>
		public virtual void SetValues<T>(long offset, long length, T value) where T : unmanaged
		{
			try
			{
				if (offset < 0)
					throw new ArgumentOutOfRangeException(nameof(offset), Parameter.CannotNegative);
				if (length <= 0)
					throw new ArgumentOutOfRangeException(nameof(length), Parameter.MustPositive);
				if ((offset + length) * Storage<T>.SizeOfT > this.WriteStream.Length)
					throw new ArgumentException(Parameter.WrongSize);

				// positioning
				length *= Storage<T>.SizeOfT;
				offset *= Storage<T>.SizeOfT;
				if (offset != this.WriteStream.Position)
					this.WriteStream.Position = offset;
				// writing
				int BufferSize = BufferSizeInBytes<T>();
				byte[] buffer = new byte[Math.Min(BufferSize, length)];
				if (value is byte bv)
				{
					Array.Fill(buffer, bv);
				}
				else
				{
					T[] bufferT = new T[buffer.Length / Storage<T>.SizeOfT];
					Array.Fill(bufferT, value);
					Buffer.BlockCopy(bufferT, 0, buffer, 0, buffer.Length);
				}
				while (length >= BufferSize)
				{
					this.WriteStream.Write(buffer, 0, BufferSize);
					length -= BufferSize;
				}
				this.WriteStream.Write(buffer, 0, (int)length);
				this.WriteStream.Flush();
			}
			finally
			{
				this.OnWriteFinish();
			}
		}

		/// <summary>
		/// Write values to this <see cref="AbstractStreamPointer"/> starting from <paramref name="offset"/>
		/// </summary>
		/// <param name="offset">The offset in bytes to start writing</param>
		/// <param name="value">The values to write as a <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/></param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> exceeds the boundary</exception>
		/// <exception cref="IOException">If an I/O error occurs</exception>
		/// <exception cref="NotSupportedException">If the <see cref="WriteStream"/> does not support seeking (when <c><see cref="ReadStream"/>.<see cref="Stream.Position">Offset</see> != <paramref name="offset"/></c>) or writing</exception>
		/// <exception cref="ObjectDisposedException">If the <see cref="WriteStream"/> is already closed</exception>
		/// <remarks>When overridden by derived classes, <see cref="OnWriteFinish"/> shall be invoked at last.</remarks>
		public virtual void Write(long offset, ReadOnlySpan<byte> value)
		{
			try
			{
				if (offset < 0)
					throw new ArgumentOutOfRangeException(nameof(offset), Parameter.CannotNegative);
				if (value.Length <= 0)
					throw new ArgumentOutOfRangeException(nameof(value), Parameter.MustPositive);
				if (offset >= this.WriteStream.Length)
					throw new ArgumentOutOfRangeException(nameof(offset), Parameter.InvalidValue);

				if (offset != this.WriteStream.Position)
					this.WriteStream.Position = offset;
				this.WriteStream.Write(value);
				this.WriteStream.Flush();
			}
			finally
			{
				this.OnWriteFinish();
			}
		}

		/// <summary>
		/// Read values from this <see cref="AbstractStreamPointer"/> starting from <paramref name="offset"/>
		/// </summary>
		/// <param name="offset">The offset in bytes to start reading</param>
		/// <param name="target">The target <see cref="Span{T}"/> of <see cref="byte"/> to overwrite the read values</param>
		/// <returns>The actual number of values read</returns>
		/// <exception cref="ArgumentException">If the <see cref="ReadStream"/> is too short</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> exceeds the boundary</exception>
		/// <exception cref="IOException">If an I/O error occurs</exception>
		/// <exception cref="NotSupportedException">If the <see cref="ReadStream"/> does not support seeking (when <c><see cref="ReadStream"/>.<see cref="Stream.Position">Offset</see> != <paramref name="offset"/></c>) or reading</exception>
		/// <exception cref="ObjectDisposedException">If the <see cref="ReadStream"/> is already closed</exception>
		/// <remarks>When overridden by derived classes, <see cref="OnReadFinish"/> shall be invoked at last.</remarks>
		public virtual int Read(long offset, Span<byte> target)
		{
			try
			{
				if (offset < 0)
					throw new ArgumentOutOfRangeException(nameof(offset), Parameter.CannotNegative);
				if (target.Length <= 0)
					throw new ArgumentOutOfRangeException(nameof(target), Parameter.MustPositive);
				if (offset + target.Length > this.ReadStream.Length)
					throw new ArgumentException(Parameter.WrongSize);

				if (offset != this.ReadStream.Position)
					this.ReadStream.Position = offset;
				return this.ReadStream.Read(target);
			}
			finally
			{
				this.OnReadFinish();
			}
		}

		/// <summary>
		/// Resize this <see cref="AbstractStreamPointer"/> to a new length in bytes.<br/>
		/// If <paramref name="newLengthInBytes"/> is smaller than <see cref="IPointer.LengthInBytes"/>, the bytes after <paramref name="newLengthInBytes"/> will be discarded; otherwise, uninitialized (<paramref name="newLengthInBytes"/> - <see cref="IPointer.LengthInBytes"/>) bytes will be added to the end.
		/// </summary>
		/// <param name="newLengthInBytes">The new length in bytes</param>
		/// <exception cref="IOException">If an I/O error occurs</exception>
		/// <exception cref="NotSupportedException">If the <see cref="WriteStream"/> does not support seeking or writing</exception>
		/// <exception cref="ObjectDisposedException">If the <see cref="WriteStream"/> is already closed</exception>
		/// <remarks>When overridden by derived classes, <see cref="OnWriteFinish"/> shall be invoked at last.</remarks>
		public virtual void Resize(ulong newLengthInBytes)
		{
			try
			{
				this.WriteStream.SetLength((long)newLengthInBytes);
				this.WriteStream.Flush();
			}
			finally
			{
				this.OnWriteFinish();
			}
		}

		/// <summary>
		/// Copy to another <see cref="AbstractStreamPointer"/> <paramref name="other"/> with given offsets and length
		/// </summary>
		/// <param name="offsetThis">The offset in bytes to start reading of this <see cref="AbstractStreamPointer"/></param>
		/// <param name="other">The other <see cref="AbstractStreamPointer"/> to write to</param>
		/// <param name="offsetOther">The offset in bytes to start writing of <paramref name="other"/> <see cref="AbstractStreamPointer"/></param>
		/// <param name="length">The length in bytes to read/write</param>
		/// <exception cref="ArgumentException">If either <see cref="ReadStream"/> is too short</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="offsetThis"/> or <paramref name="offsetOther"/> or <paramref name="length"/> exceeds the boundaries</exception>
		/// <exception cref="IOException">If an I/O error occurs</exception>
		/// <exception cref="NotSupportedException">If <see cref="ReadStream"/> of this or <see cref="WriteStream"/> of <paramref name="other"/> does not support seeking (when <c>this.<see cref="ReadStream"/>.<see cref="Stream.Position">Offset</see> != <paramref name="offsetThis"/></c> or <c><paramref name="other"/>.<see cref="WriteStream"/>.<see cref="Stream.Position">Offset</see> != <paramref name="offsetOther"/></c>) or reading</exception>
		/// <exception cref="ObjectDisposedException">If <see cref="ReadStream"/> of this or <see cref="WriteStream"/> is already closed</exception>
		/// <remarks>When overridden by derived classes, <see cref="OnReadFinish"/> of this and <see cref="OnWriteFinish"/> of <paramref name="other"/> shall be invoked at last.</remarks>
		public virtual void CopyTo(long offsetThis, AbstractStreamPointer other, long offsetOther, long length)
		{
			try
			{
				if (offsetThis < 0)
					throw new ArgumentOutOfRangeException(nameof(offsetThis), Parameter.CannotNegative);
				if (offsetOther < 0)
					throw new ArgumentOutOfRangeException(nameof(offsetOther), Parameter.CannotNegative);
				if (length <= 0)
					throw new ArgumentOutOfRangeException(nameof(length), Parameter.MustPositive);
				if (offsetThis + length > this.ReadStream.Length)
					throw new ArgumentException(Parameter.WrongSize);
				if (offsetOther >= other.WriteStream.Length)
					throw new ArgumentOutOfRangeException(nameof(offsetOther), Parameter.InvalidValue);

				// positioning
				if (offsetThis != this.ReadStream.Position)
					this.ReadStream.Position = offsetThis;
				if (offsetOther != other.WriteStream.Position)
					other.WriteStream.Position = offsetOther;
				// writing
				int BufferSize = BufferSizeInBytes<byte>();
				byte[] buffer = new byte[Math.Min(BufferSize, length)];
				while (length > 0)
				{
					int read = this.ReadStream.Read(buffer, 0, buffer.Length);
					other.WriteStream.Write(buffer, 0, read);
					length -= read;
				}
				other.WriteStream.Flush();
			}
			finally
			{
				this.OnReadFinish(); other.OnWriteFinish();
			}
		}
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
		public override bool Equals(Storage<T> obj)
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
		/// <param name="cacheUri">the final caching indicated by a <see cref="Uri"/>, default null means do not cache to URI</param>
		/// <exception cref="ArgumentException">if <paramref name="priorities"/> has unexpected value(s) or is of wrong size</exception>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="totalLength"/> or <paramref name="cacheUri"/> has unexpected value(s)</exception>
		public CachedStorage(IEnumerable<(StorageLocation location, ulong maxLengthInBytes)> priorities, ulong totalLength, Uri cacheUri = null) : base(totalLength)
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
			// deal with URI
			if (cacheUri is not null)
			{
				UriScheme? scheme = cacheUri.GetScheme();
				if (!scheme.HasValue)
					throw new ArgumentOutOfRangeException(nameof(cacheUri), Parameter.InvalidValue);
				// do not allocate here
				temp.Add(new StoragePointer(new StorageLocation(LocationType.Uri, (byte)scheme.Value), default(IntPtr), totalLength <= allowedTotalLength ? 0 : totalLength - allowedTotalLength));
			}
			this.pointers = temp.ToArray();
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
