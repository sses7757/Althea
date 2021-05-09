using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.Helpers;
using Althea.Linq;
using Althea.NativeTypes;
using Althea.Resources;

using MEM = Althea.Storage.AbstractApi;


namespace Althea.Backend.Storage
{
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool ICheckValid.IsValid() => this.Pointer != default && this.LengthInBytes > 0;

		string IMainPropertyFormat.StringMain {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.Pointer.ToString("X");
		}

		IEnumerable<KeyValuePair<string, object?>> IMainPropertyFormat.StringProperties {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new Dictionary<string, object?>(1)
			{
				[nameof(LengthInBytes)] = this.LengthInBytes
			};
		}
		#endregion
	}

	/// <summary>
	/// The abstract class for any possible stream storage that can read, seek (and possibly write)
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
		public abstract long Position { get; set; }

		/// <summary>
		/// When implemented by a derived class, get or set the length in bytes of this <see cref="Stream"/>
		/// </summary>
		public long Length { get; }

		/// <summary>
		/// Create a <see cref="Stream"/> with given <paramref name="length"/> in bytes
		/// </summary>
		/// <param name="length">The given length in bytes</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="length"/> is not a positive value</exception>
		protected Stream(long length)
		{
			if (length <= 0)
				throw new ArgumentOutOfRangeException(nameof(length), length, Parameter.MustPositive);
			this.Length = length;
		}

		/// <summary>
		/// When implemented by a derived class, get a <see cref="bool"/> indicating whether this <see cref="Stream"/> can transfer data with managed C# memory directly or not.
		/// </summary>
		public abstract bool CanTransferWithManaged { get; }

		/// <summary>
		/// When implemented by a derived class, get a <see cref="bool"/> indicating whether user can write data to this <see cref="Stream"/>.
		/// </summary>
		public abstract bool CanWrite { get; }

		/// <summary>
		/// When implemented by a derived class, <b>statically</b> get the supported data transfer locations represented by <see cref="StorageLocation"/>s of this <see cref="Stream"/>
		/// </summary>
		/// <remarks>When implemented by a derived class, if this property returns null or empty list, <see cref="NullReferenceException"/> may be thrown</remarks>
		protected abstract IReadOnlyList<StorageLocation> SupportedTransfers { get; }

		/// <summary>
		/// When implemented by a derived class, <b>statically</b> get a <see cref="bool"/> indicating whether data transfer with given <paramref name="location"/> is supported by this <see cref="Stream"/>. The default implementation utilizes the <see cref="SupportedTransfers"/>.
		/// </summary>
		/// <param name="location">The given <see cref="StorageLocation"/> to check transfer supporting</param>
		/// <returns>Whether data transfer with <paramref name="location"/> is supported or not</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
		/// <exception cref="UnauthorizedAccessException">If this <see cref="Stream"/> was created read-only</exception>
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
		/// <exception cref="UnauthorizedAccessException">If this <see cref="Stream"/> was created read-only</exception>
		public abstract void FromMemory(PointerSegment memory);

		/// <summary>
		/// When implemented by a derived class, read data from the given <paramref name="managed"/> memory as a<see cref="Span{T}"/> and write them to this <see cref="Stream"/> started from <see cref="Position"/>.
		/// </summary>
		/// <param name="managed">The managed memory as a <see cref="ReadOnlySpan{T}"/> to read from</param>
		/// <remarks>When finished, the <see cref="Position"/> shall be advanced by the number of bytes written.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="managed"/> is not valid (for example, has zero length)</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="managed"/>'s length exceeds the boundary</exception>
		/// <exception cref="NotSupportedException">If <see cref="CanTransferWithManaged"/> is false</exception>
		/// <exception cref="System.IO.IOException">If a general I/O error occurred</exception>
		/// <exception cref="ObjectDisposedException">If this is already disposed</exception>
		/// <exception cref="UnauthorizedAccessException">If this <see cref="Stream"/> was created read-only</exception>
		public abstract void FromManged<T>(ReadOnlySpan<T> managed) where T : unmanaged;
		#endregion

		#region default implementation
		/// <summary>
		/// Get the default buffer size in bytes which is divisible by the size of <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">any unmanaged data type</typeparam>
		/// <returns>The default buffer size in bytes divisible by the size of <typeparamref name="T"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static int BufferSizeInBytes<T>() where T : unmanaged => (1 << 16) / Const<T>.SizeT * Const<T>.SizeT;

		private static readonly Dictionary<Type, StorageLocation> cache_single_location = new();

		/// <summary>
		/// Check the parameters of <see cref="SetValues{T}(T, long)"/>
		/// </summary>
		/// <returns><paramref name="length"/> * size of <typeparamref name="T"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected long SetValuesCheck<T>(long length) where T : unmanaged
		{
			if (this.Disposed)
				throw new ObjectDisposedException(this.GetType().FullName);
			if (!this.CanWrite)
				throw new UnauthorizedAccessException(string.Format(Resource.CannotWrite, this.GetType().GetGenericString()));
			if (length <= 0)
				throw new ArgumentOutOfRangeException(nameof(length), length, Parameter.MustPositive);
			length *= Const<T>.SizeT;
			if (this.Position + length > this.Length)
				throw new ArgumentOutOfRangeException(nameof(length), length, Parameter.InvalidValue);
			return length;
		}

		/// <summary>
		/// When overridden in a derived class, fill some values of this <see cref="Stream"/> of given <paramref name="length"/>. The default implementation tries to use the managed buffer or buffer allocated on the found first intersection of both <see cref="SupportedTransfers"/>.
		/// </summary>
		/// <typeparam name="T">any unmanaged data type</typeparam>
		/// <param name="value">The value of type <typeparamref name="T"/> to be set</param>
		/// <param name="length">The length in <typeparamref name="T"/></param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="length"/> exceeds any of the boundaries</exception>
		/// <exception cref="System.IO.IOException">If an I/O error occurs</exception>
		/// <exception cref="ObjectDisposedException">If this is already disposed</exception>
		/// <exception cref="UnauthorizedAccessException">If this <see cref="Stream"/> was created read-only</exception>
		public virtual void SetValues<T>(T value, long length) where T : unmanaged
		{
			SetValuesCheck<T>(length);

			int bufferSize = BufferSizeInBytes<T>() / Const<T>.SizeT;
			if (this.CanTransferWithManaged)
			{
				int len = checked((int)length);
				int bufferLength = Math.Min(bufferSize, len);
				Span<T> buffer = bufferLength <= Settings.StackAllocLimit ? stackalloc T[bufferLength] : new T[bufferLength];
				buffer.Fill(value);
				while (len > 0)
				{
					var span = buffer.Slice(0, Math.Min(len, buffer.Length));
					this.FromManged<T>(span);
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
				long len = Math.Min(bufferSize, length);
				using var pointer = Storage<T>.Create(location, len);
				MEM.FillWithValue(pointer, value);
				while (len > 0)
				{
					Storage<T> temp = pointer;
					if (len < pointer.Length)
						temp = pointer.MakeReference(newLength: len);
					this.FromMemory(temp[0]);
					len -= pointer.Length;
				}
			}
			// flush at the end
			this.Flush();
		}

		private static readonly Dictionary<ImmutableTwoElementSet<RuntimeTypeHandle>, StorageLocation> cache_double_location = new();

		/// <summary>
		/// Check the parameters of <see cref="CopyTo(Stream, long)"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void CopyToCheck(Stream other, long length)
		{
			if (length <= 0)
				throw new ArgumentOutOfRangeException(nameof(length), length, Parameter.MustPositive);
			if (this.Disposed)
				throw new ObjectDisposedException(this.GetType().FullName);
			if (other.Disposed)
				throw new ObjectDisposedException(other.GetType().FullName);
			if (!other.CanWrite)
				throw new UnauthorizedAccessException(string.Format(Resource.CannotWrite, other.GetType().GetGenericString()));
			if (this.Position + length > this.Length)
				throw new ArgumentOutOfRangeException(nameof(length), length, Parameter.InvalidValue);
			if (other.Position + length > other.Length)
				throw new ArgumentOutOfRangeException(nameof(length), length, Parameter.InvalidValue);
		}

		/// <summary>
		/// When overridden in a derived class, copy some data from this <see cref="Stream"/> to <paramref name="other"/> <see cref="Stream"/> of given <paramref name="length"/>. The default implementation tries to use the managed buffer or buffer allocated on the found first intersection of both <see cref="SupportedTransfers"/>.
		/// </summary>
		/// <param name="other">The other <see cref="Stream"/> to copy to</param>
		/// <param name="length">The length in bytes to copy</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="length"/> exceeds any of the boundaries</exception>
		/// <exception cref="NotSupportedException">If there are not common supported data transfers between this and <paramref name="other"/></exception>
		/// <exception cref="System.IO.IOException">If an I/O error occurs</exception>
		/// <exception cref="ObjectDisposedException">If this or <paramref name="other"/> is already disposed</exception>
		/// <exception cref="UnauthorizedAccessException">If the <paramref name="other"/> <see cref="Stream"/> was created read-only</exception>
		public virtual void CopyTo(Stream other, long length)
		{
			CopyToCheck(other, length);

			int bufferSize = BufferSizeInBytes<byte>();
			if (this.CanTransferWithManaged && other.CanTransferWithManaged)
			{
				int len = checked((int)length);
				byte[] buffer = new byte[Math.Min(bufferSize, len)];
				while (len > 0)
				{
					var span = buffer.AsSpan(0, Math.Min(len, buffer.Length));
					this.ToManged(span);
					other.FromManged<byte>(span);
					len -= span.Length;
				}
			}
			else
			{
				// get StorageLocation cache
				var key = new ImmutableTwoElementSet<RuntimeTypeHandle>(this.GetType().TypeHandle, other.GetType().TypeHandle);
				StorageLocation value;
				if (cache_double_location.ContainsKey(key))
				{
					value = cache_double_location[key];
				}
				else
				{
					var intersect = this.SupportedTransfers.FirstIntersect(other.SupportedTransfers);
					if (intersect == default)
						throw new NotSupportedException(Support.Location);
					cache_double_location.Add(key, intersect);
					value = intersect;
				}
				// copy
				long len = Math.Min(bufferSize, length);
				using var pointer = Storage<byte>.Create(value, len);
				while (len > 0)
				{
					Storage<byte> temp = pointer;
					if (len < pointer.Length)
						temp = pointer.MakeReference(newLength: len);
					this.ToMemory(temp[0]);
					other.FromMemory(temp[0]);
					len -= pointer.Length;
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
		long IPointer.LengthInBytes {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.NativeStream.Length;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool ICheckValid.IsValid() => this.NativeStream.IsValid();

		IEnumerable<KeyValuePair<string, object?>> IMainPropertyFormat.StringProperties {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new Dictionary<string, object?>(2)
			{
				[nameof(this.LengthInBytes)] = this.NativeStream.Length,
				[nameof(Stream.Position)] = this.NativeStream.Position,
			};
		}
		#endregion
	}
}
