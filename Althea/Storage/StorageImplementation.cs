using System;
using System.IO;
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
		/// Specifies that the URI is accessed through the File Transfer Protocol (FTP).
		/// </summary>
		FTP = 2,
		/// <summary>
		/// Specifies that the URI is accessed through the TCP/IP directly.
		/// </summary>
		TCP = 3,
	}

	/// <summary>
	/// The static class for extension methods of <see cref="UriScheme"/>
	/// </summary>
	public static class UriSchemeExtension
	{
		/// <summary>
		/// Get the <see cref="UriScheme"/> from a <see cref="Uri"/>
		/// </summary>
		/// <param name="uri">the absolute <see cref="Uri"/></param>
		/// <returns>the <see cref="UriScheme"/> of <paramref name="uri"/>, or null if <paramref name="uri"/>'s scheme is not in <see cref="UriScheme"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="uri"/> is not an absolute URI</exception>
		public static UriScheme? GetScheme(this Uri uri)
		{
			if (!uri.IsAbsoluteUri)
				throw new ArgumentOutOfRangeException(nameof(uri));
			if (uri.Scheme == Uri.UriSchemeFile)
				return UriScheme.File;
			if (uri.Scheme == Uri.UriSchemeFtp)
				return UriScheme.FTP;
			if (uri.Scheme == @"tcp")
				return UriScheme.TCP;
			return null;
		}
	}
	#endregion


	#region interfaces
	/// <summary>
	/// The interface for an immutable pointer at any possible memory storage which can be described by a <see cref="IntPtr"/>
	/// </summary>
	public interface IMemoryPointer : IPointer
	{
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
	}

	/// <summary>
	/// The interface for an immutable pointer at any possible stream storage which can be described by a <see cref="System.IO.Stream"/>
	/// </summary>
	public interface IStreamPointer : IPointer
	{
		/// <summary>
		/// Get the raw stream of this <see cref="IStreamPointer"/> as a <see cref="Stream"/>
		/// </summary>
		Stream Stream { get; }

		/// <summary>
		/// The basic description of this <see cref="IStreamPointer"/> as a <see cref="string"/>, such as <see cref="Uri.ToString"/>
		/// </summary>
		string Description { get; }

		bool ICheckValid.IsValid() => this.Stream is not null && this.LengthInBytes != 0;

		bool IPointer.IsValidLocation(StorageLocation location) => location.Location.GetClassification() == LocationTypeExtension.ClassStream;

		bool IPointer.CanRead => this.Stream.CanRead;

		bool IPointer.CanWrite => this.Stream.CanWrite;

		bool IPointer.CanReadOffset => this.Stream.CanSeek && this.Stream.CanRead;

		bool IPointer.CanWriteOffset => this.Stream.CanSeek && this.Stream.CanWrite;

		bool IPointer.CanResize => this.Stream.CanWrite;

		string IMainPropertyFormat.StringMain => this.Description;

		IReadOnlyDictionary<string, string> IMainPropertyFormat.StringProperties => new Dictionary<string, string>
		{ 
			["length"] = this.LengthInBytes.ToString(),
			["position"] = this.Stream.Position.ToString(),
		};
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
		/// Allocate a <see cref="StoragePointer"/> of given <see cref="Storage{T}.Length"/> on given <see cref="StorageLocation"/> 
		/// </summary>
		/// <param name="location">a <see cref="StorageLocation"/> to represent the memory location</param>
		/// <param name="length">the length of contiguous memory block in <typeparamref name="T"/></param>
		protected static StoragePointer Allocate(StorageLocation location, ulong length)
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
		private readonly StoragePointer pointer;

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
		/// The number of <see cref="StoragePointer"/>(s) of this <see cref="Storage{T}"/> 
		/// </summary>
		public override int Count => 1;

		/// <summary>
		/// Indexer of the <see cref="StoragePointer"/>(s) of this <see cref="Storage{T}"/> (in presenting order)
		/// </summary>
		/// <param name="index">the element index</param>
		/// <returns>the <see cref="StoragePointer"/> at <paramref name="index"/></returns>
		public override StoragePointer this[int index] {
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
		private readonly StoragePointer[] pointers;

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
			this.pointers = new StoragePointer[param.Count];
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
		/// The number of <see cref="StoragePointer"/>(s) of this <see cref="Storage{T}"/> 
		/// </summary>
		public override int Count => this.pointers.Length;

		/// <summary>
		/// Indexer of the <see cref="StoragePointer"/>(s) of this <see cref="Storage{T}"/> (in presenting order)
		/// </summary>
		/// <param name="index">the element index</param>
		/// <returns>the <see cref="StoragePointer"/> at <paramref name="index"/></returns>
		public override StoragePointer this[int index] {
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
		private readonly StoragePointer[] pointers;

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
			var temp = new List<StoragePointer>();
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
		/// The number of <see cref="StoragePointer"/>(s) of this <see cref="Storage{T}"/> 
		/// </summary>
		public override int Count => this.pointers.Length;

		/// <summary>
		/// Indexer of the <see cref="StoragePointer"/>(s) of this <see cref="Storage{T}"/> (in presenting order)
		/// </summary>
		/// <param name="index">the element index</param>
		/// <returns>the <see cref="StoragePointer"/> at <paramref name="index"/></returns>
		public override StoragePointer this[int index] {
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
