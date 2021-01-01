using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.Linq;
using Althea.Helpers;

using RT = Althea.Runtime.API;


namespace Althea.Memory
{
	#region storage location
	/// <summary>
	/// The enum of the storage location, a bit flag
	/// </summary>
	/// <remarks>Use <see cref="StorageLocationExtension.StringRepr(StorageLocation)"/> to get the real string representations.<br/>
	/// Other memory locations with higher ranks are not explicitly written (such as 1 &lt;&lt; 5), but they will still be correctly dealt with.</remarks>
	[Flags]
	public enum StorageLocation
	{
		/// <summary>
		/// The "memory" storage determined by a <see cref="Uri"/>. The value 0 means when no other flag is set, the storage location is <see cref="URI"/>.
		/// </summary>
		URI = 0,
		/// <summary>
		/// Storage at local CPU memory
		/// </summary>
		CpuMemory = 1 << 0,
		/// <summary>
		/// Storage at local GPU memory
		/// </summary>
		GpuMemory = 1 << 1,
		/// <summary>
		/// Storage at platform-specific local memory (with custom order the 1st) other than <see cref="CpuMemory"/> and <see cref="GpuMemory"/>. For example, a RAM associated with a FPGA.
		/// </summary>
		OtherMemory_1 = 1 << 2,
		/// <summary>
		/// Storage at platform-specific local memory (with custom order the 2nd) other than <see cref="CpuMemory"/> and <see cref="GpuMemory"/>. For example, a RAM associated with a FPGA.
		/// </summary>
		OtherMemory_2 = 1 << 3,
		/// <summary>
		/// Storage at platform-specific local memory (with custom order the 3rd) other than <see cref="CpuMemory"/> and <see cref="GpuMemory"/>. For example, a RAM associated with a FPGA.
		/// </summary>
		OtherMemory_3 = 1 << 4,
	}

	/// <summary>
	/// The static class that contains several extension methods for <see cref="StorageLocation"/>
	/// </summary>
	public static class StorageLocationExtension
	{
		private static readonly Dictionary<StorageLocation, string> _otherMemoryNames = new Dictionary<StorageLocation, string>();

		/// <summary>
		/// Set the name used for <see cref="StringRepr(StorageLocation)"/> of the given flag <see cref="StorageLocation"/> if it represents a storage position in other memory types like <see cref="StorageLocation.OtherMemory_1"/>
		/// </summary>
		/// <param name="position">the flag <see cref="StorageLocation"/> of a storage position in other memory types</param>
		/// <param name="name">the name as a <see cref="string"/> to set; notice that all the spaces will be replaced by '_'</param>
		/// <returns>success or not</returns>
		public static bool SetOtherMemoryName(this StorageLocation position, string name)
		{
			if (position < StorageLocation.OtherMemory_1 || !position.IsPureFlag())
				return false;
			_otherMemoryNames[position] = name.Replace(' ', '_');
			return true;
		}

		/// <summary>
		/// Check whether the given <see cref="StorageLocation"/> is a pure flag
		/// </summary>
		/// <param name="position">the <see cref="StorageLocation"/></param>
		/// <returns>true for pure flag <paramref name="position"/></returns>
		public static bool IsPureFlag(this StorageLocation position)
		{
			return ((int)position).IsPowerOfTwo();
		}

		/// <summary>
		/// Get the ID (order) for the given flag <see cref="StorageLocation"/> if it represents a storage position in other memory types like <see cref="StorageLocation.OtherMemory_1"/>
		/// </summary>
		/// <param name="position">the flag <see cref="StorageLocation"/> of a storage position in other memory types</param>
		/// <returns>-1 if <paramref name="position"/> is not a flag or it is not a memory of other types</returns>
		public static int OtherMemoryTypeID(this StorageLocation position)
		{
			if (position < StorageLocation.OtherMemory_1 || !position.IsPureFlag())
				return -1;
			int id = ((int)position).Log2() - 1;
			return id;
		}

		/// <summary>
		/// Decompose the given <see cref="StorageLocation"/> to flags
		/// </summary>
		/// <param name="position">the <see cref="StorageLocation"/></param>
		/// <returns>the flags in <see cref="StorageLocation"/></returns>
		public static StorageLocation[] Decompose(this StorageLocation position)
		{
			int pos = (int)position;
			sbyte max = pos.Log2();
			List<StorageLocation> flags = new List<StorageLocation>(max) { (StorageLocation)max };
			while ((pos = pos.ResetBit(max)) != 0)
			{
				max = pos.Log2();
				flags.Add((StorageLocation)max);
			}
			return flags.ToArray();
		}

		/// <summary>
		/// Get the string representation of a given <see cref="StorageLocation"/>
		/// </summary>
		/// <param name="position">the <see cref="StorageLocation"/></param>
		/// <returns>the string representation of <paramref name="position"/></returns>
		public static string StringRepr(this StorageLocation position)
		{
			if (position == StorageLocation.URI)
			{
				return "URI";
			}
			else if (position.IsPureFlag())
			{
				return position switch
				{
					StorageLocation.CpuMemory => "CPU_Memory",
					StorageLocation.GpuMemory => "GPU_Memory",
					_ => _otherMemoryNames.GetValueOrDefault(position) ?? $"Other_Device_Memory(ID={position.OtherMemoryTypeID()})",
				};
			}
			else
			{
				return string.Join(" & ", position.Decompose().Select(p => p.StringRepr()).ToArray());
			}
		}

		// TODO: edit CheckOnHost
		internal static bool CheckOnHost<T>(params Arrays.ValueArray<T>[] arrays) where T : unmanaged, IFormattable, IEquatable<T>
		{
			if (arrays is null || arrays.Length == 0)
				throw new ArgumentNullException(nameof(arrays));
			if (arrays.Any(a => a.Disposed))
				throw new ObjectDisposedException(nameof(arrays));
			if (arrays.All(a => a.Length == 0 || !a.OnHost)) // empty array can be any where
				return false;
			if (arrays.All(a => a.Length == 0 || a.OnHost))
				return true;
			// else
			throw new ArgumentException(Resource.RequireSamePos);
		}

		internal static bool CheckOnHost<T>(params Memory.Storage<T>[] arrays) where T : unmanaged, IFormattable, IEquatable<T>
		{
			if (arrays is null || arrays.Length == 0)
				throw new ArgumentNullException(nameof(arrays));
			////if (arrays.Any(a => a.AlreadyDisposed))
			////	throw new ObjectDisposedException(nameof(arrays));
			if (arrays.All(a => !a.OnHost))
				return false;
			if (arrays.All(a => a.OnHost))
				return true;
			// else
			throw new ArgumentException(Resource.RequireSamePos);
		}

	}
	#endregion


	#region storage interfaces
	/// <summary>
	/// The interface for a storage of any type at any location
	/// </summary>
	public interface IStorage : IDisposable
	{
		/// <summary>
		/// The <see cref="StorageLocation"/> of this storage
		/// </summary>
		StorageLocation Location { get; }

		/// <summary>
		/// The raw pointer as a <see cref="IntPtr"/>
		/// </summary>
		IntPtr Ptr { get; }

		/// <summary>
		/// The length of this pointer's underlying array in bytes
		/// </summary>
		long LengthInBytes { get; }
	}

	/// <summary>
	/// The interface for a storage of data type <typeparamref name="T"/> at any location
	/// </summary>
	/// <typeparam name="T">any unmanaged struct</typeparam>
	public interface IStorage<T> : IStorage where T : unmanaged
	{
		/// <summary>
		/// The C# managed pointer as a <c>ref <typeparamref name="T"/></c>
		/// </summary>
		ref T Pointer { get; }

		/// <summary>
		/// The length of this pointer's underlying array in <typeparamref name="T"/> rather than bytes
		/// </summary>
		long Length { get; }

		/// <summary>
		/// Get the size of <typeparamref name="T"/> in memory in bytes
		/// </summary>
		public static unsafe int SizeOfT { get; } = sizeof(T);

		/// <summary>
		/// Make a reference <see cref="IStorage{T}"/> with the same pointer as this one while <see cref="Length"/> is changed to <paramref name="newLength"/>
		/// </summary>
		/// <param name="newLength">the new length of referenced <see cref="Storage{T}"/></param>
		/// <returns>a referenced <see cref="IStorage{T}"/>with different <see cref="Length"/></returns>
		IStorage<T> MakeReferenceWithSize(long newLength);

		/// <summary>
		/// Resize this <see cref="Storage{T}"/> <b>in-place</b>, if <c><paramref name="newLength"/> &lt; <see cref="Length"/></c>, the elements with larger offsets will be removed; otherwise, some new arbitrary elements will be attached to the end.
		/// </summary>
		/// <param name="newLength">the new length to resize to</param>
		void Resize(long newLength);

		/// <summary>
		/// Convert this <see cref="IStorage{T}"/> to another one with different data type <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">the output data type</typeparam>
		/// <returns>a referenced <see cref="IStorage{TOut}"/></returns>
		Storage<TOut> As<TOut>() where TOut : unmanaged;
	}
	#endregion


	/// <summary>
	/// The abstract wrapper class of raw device / host pointer <see cref="IntPtr"/>.
	/// </summary>
	/// <typeparam name="T">any unmanaged struct</typeparam>
	/// <remarks>I must warn you that although C# has GC to periodically collect unused garbage to prevent memory leak, you should not rely on it too much. <b>Remember</b> to use <c>using</c> statement or call <see cref="Storage{T}.Dispose()"/>.<br/>
	/// The leaked memory which will be collected GC still causes not only performance loss but also potential bugs if you do not know how GC works, since the concrete class that inherits <see cref="ISwappablePointer"/> shall be a class with finalizers thus cannot be in GC generation 0, i.e. it will not be immediately disposed when out-of-scope.<br/>
	/// See https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/ for official documentations of GC of dot NET.</remarks>
	public abstract class Storage<T> : IStorage, IEquatable<Storage<T>> where T : unmanaged
	{
		#region properties
		/// <summary>
		/// The raw pointer
		/// </summary>
		public abstract IntPtr Ptr { get; protected set; }

		/// <summary>
		/// The length of this pointer's underlying array in <typeparamref name="T"/>
		/// </summary>
		public abstract long Length { get; protected set; }

		/// <summary>
		/// The length of this pointer's underlying array in bytes
		/// </summary>
		public long LengthInBytes => this.Length * SizeOfT;

		/// <summary>
		/// This pointer is on host or device memory
		/// </summary>
		public abstract bool OnHost { get; protected set; }

		/// <summary>
		/// Get the size of <typeparamref name="T"/> in memory in bytes
		/// </summary>
		public static long SizeOfT { get; } = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
		#endregion

		#region memory copy kind info
		/// <summary>
		/// Get the <see cref="MemoryCopyKind"/> for copying to another <see cref="Storage{T}"/>.
		/// </summary>
		/// <param name="another">the <see cref="Storage{T}"/> to copy to</param>
		/// <returns>the corresponding <see cref="MemoryCopyKind"/></returns>
		public MemoryCopyKind CopyToKind(Storage<T> another)
		{
			if (another is null || another.Ptr == default)
				throw new ArgumentNullException(nameof(another));
			return this.CopyToKind(another.OnHost);
		}

		/// <summary>
		/// Get the <see cref="MemoryCopyKind"/> for copying to another position.
		/// </summary>
		/// <param name="anotherOnHost">the position to copy to is on host memory or device</param>
		/// <returns>the corresponding <see cref="MemoryCopyKind"/></returns>
		public MemoryCopyKind CopyToKind(bool anotherOnHost)
		{
			return CopyKind(this.OnHost, anotherOnHost);
		}

		/// <summary>
		/// Get the <see cref="MemoryCopyKind"/> for copying from another position.
		/// </summary>
		/// <param name="anotherOnHost">the position to copy from is on host memory or device</param>
		/// <returns>the corresponding <see cref="MemoryCopyKind"/></returns>
		public MemoryCopyKind CopyFromKind(bool anotherOnHost)
		{
			return CopyKind(anotherOnHost, this.OnHost);
		}

		private static MemoryCopyKind CopyKind(bool fromOnHost, bool toOnHost)
		{
			if (fromOnHost && toOnHost)
				return MemoryCopyKind.HostToHost;
			if (fromOnHost && !toOnHost)
				return MemoryCopyKind.HostToDevice;
			if (!fromOnHost && toOnHost)
				return MemoryCopyKind.DeviceToHost;
			if (!fromOnHost && !toOnHost)
				return MemoryCopyKind.DeviceToDevice;
			return MemoryCopyKind.Default;
		}
		#endregion

		#region dispose
		/// <summary>
		/// If this storage is disposed or not
		/// </summary>
		protected bool Disposed { get; set; }

		/// <summary>
		/// Dispose method to release resources.
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
		}

		/// <summary>
		/// The actual dispose method
		/// </summary>
		/// <param name="disposeManaged">dispose managed resource or not</param>
		protected abstract void Dispose(bool disposeManaged);
		#endregion

		#region create
		internal delegate Storage<T> DelegateCreateNew(long length, bool onHost = false);

		internal delegate Storage<T> DelegateCreateNewWith(IntPtr ptr, long length, bool onHost = false);

		internal delegate Storage<T> DelegateCreateReference(Storage<T> storage, long offset);

		internal delegate Storage<T> DelegateCreateReferenceFull(ISwappablePointer root, long offsetInBytes, long length = -1);

		private static readonly DelegateCreateNew CreateNew = DefaultStorageFactory.Singleton.CreateNew<T>;
		private static readonly DelegateCreateNewWith CreateNewWith = DefaultStorageFactory.Singleton.CreateNewWith<T>;
		private static readonly DelegateCreateReference CreateReference = DefaultStorageFactory.Singleton.CreateReference<T>;
		private static readonly DelegateCreateReferenceFull CreateReferenceFull = DefaultStorageFactory.Singleton.CreateReferenceFull<T>;

		static Storage()
		{
			if (!(Settings.StorageFactory is null))
			{
				if (Settings.StorageFactory is IStorageFactory factory)
				{
					CreateNew = factory.CreateNew<T>;
					CreateNewWith = factory.CreateNewWith<T>;
					CreateReference = factory.CreateReference<T>;
					CreateReferenceFull = factory.CreateReferenceFull<T>;
				}
			}
			else if (!(Settings.SwappableStorage is null))
			{
				{
					var @params = new[] { Expression.Parameter(typeof(long)), Expression.Parameter(typeof(bool)) };
					var ctor = Settings.SwappableStorage.GetConstructor(Array.ConvertAll(@params, p => p.Type));
					var lambda = Expression.Lambda<DelegateCreateNew>(Expression.New(ctor, @params), @params);
					CreateNew = lambda.Compile();
				}
				{
					var @params = new[] { Expression.Parameter(typeof(IntPtr)), Expression.Parameter(typeof(long)), Expression.Parameter(typeof(bool)) };
					var ctor = Settings.SwappableStorage.GetConstructor(Array.ConvertAll(@params, p => p.Type));
					var lambda = Expression.Lambda<DelegateCreateNewWith>(Expression.New(ctor, @params), @params);
					CreateNewWith = lambda.Compile();
				}
			}
			else if (!(Settings.UnswappableStorage is null))
			{
				{
					var @params = new[] { Expression.Parameter(typeof(long)), Expression.Parameter(typeof(bool)) };
					var ctor = Settings.UnswappableStorage.GetConstructor(Array.ConvertAll(@params, p => p.Type));
					var lambda = Expression.Lambda<DelegateCreateReference>(Expression.New(ctor, @params), @params);
					CreateReference = lambda.Compile();
				}
				{
					var @params = new[] { Expression.Parameter(typeof(IntPtr)), Expression.Parameter(typeof(long)), Expression.Parameter(typeof(bool)) };
					var ctor = Settings.UnswappableStorage.GetConstructor(Array.ConvertAll(@params, p => p.Type));
					var lambda = Expression.Lambda<DelegateCreateReferenceFull>(Expression.New(ctor, @params), @params);
					CreateReferenceFull = lambda.Compile();
				}
			}
		}

		/// <summary>
		/// Create a new <see cref="Storage{T}"/> and allocate memory of given size <paramref name="length"/> on memory position <paramref name="onHost"/>.
		/// </summary>
		/// <param name="length">the size in <typeparamref name="T"/> of pointer to create</param>
		/// <param name="onHost">the memory position, on host (CPU) memory or device (GPU) memory</param>
		/// <returns>the created <see cref="Storage{T}"/></returns>
		public static Storage<T> Create(long length, bool onHost = false)
		{
			return CreateNew(length, onHost);
		}

		/// <summary>
		/// Create a new <see cref="Storage{T}"/> with given pointer <paramref name="ptr"/>, size <paramref name="length"/> and memory position <paramref name="onHost"/>.
		/// </summary>
		/// <param name="ptr">the allocate pointer as a <see cref="IntPtr"/></param>
		/// <param name="length">the size in <typeparamref name="T"/> of pointer to create</param>
		/// <param name="onHost">the memory position, on host (CPU) memory or device (GPU) memory</param>
		/// <returns>the created <see cref="Storage{T}"/></returns>
		public static Storage<T> Create(IntPtr ptr, long length, bool onHost = false)
		{
			return CreateNewWith(ptr, length, onHost);
		}
		#endregion

		#region reference
		/// <summary>
		/// <b>In-place</b> move this storage to the other memory
		/// </summary>
		public abstract void ToOtherMemory();

		/// <summary>
		/// Replace this <see cref="Storage{T}"/> by <paramref name="another"/> one and destroy <paramref name="another"/> afterwards.
		/// </summary>
		/// <param name="another">the <see cref="Storage{T}"/> to replace</param>
		/// <remarks>only works when both <see cref="Storage{T}"/>s are <see cref="ISwappablePointer"/>s</remarks>
		public void ReplaceBy(Storage<T> another)
		{
			if (!(this is ISwappablePointer) || !(another is ISwappablePointer))
				throw new InvalidOperationException();
			// release original unmanaged resources of this one
			this.Dispose();
			// re-register disposition of this one
			this.Disposed = false;
			GC.ReRegisterForFinalize(this);
			// change pointer of this one
			this.Ptr = another.Ptr;
			this.Length = another.Length;
			this.OnHost = another.OnHost;
			// "dispose" another one
			another.Disposed = true;
			AutoSwapMemory.NotifyDisposeStorage((ISwappablePointer)another);
		}

		/// <summary>
		/// Create a <see cref="Storage{T}"/> as a reference of <paramref name="another"/>
		/// </summary>
		/// <param name="another">another <see cref="Storage{T}"/> to refer</param>
		/// <returns>the created <see cref="StorageView{T}"/></returns>
		/// <remarks>if this is a <see cref="ISwappablePointer"/>, this will be disposed</remarks>
		public Storage<T> MakeRefOf(Storage<T> another)
		{
			if (this is ISwappablePointer at)
			{
				this.Dispose();
				AutoSwapMemory.NotifyDisposeStorage(at);
			}
			return CreateReferenceFull(another.GetRoot(), another.GetOffset(), another.Length);
		}
		#endregion

		#region resize
		/// <summary>
		/// Get the root <see cref="ISwappablePointer"/> of this <see cref="Storage{T}"/> (this one if it is <see cref="ISwappablePointer"/>).
		/// </summary>
		/// <returns>the root <see cref="ISwappablePointer"/> of this <see cref="Storage{T}"/></returns>
		public ISwappablePointer GetRoot() => this is ISwappablePointer at ? at : ((IUnswappablePointer)this).Root;

		/// <summary>
		/// Get the offset in bytes of this <see cref="Storage{T}"/> (zero if it is <see cref="ISwappablePointer"/>).
		/// </summary>
		/// <returns>the offset in bytes of this <see cref="Storage{T}"/></returns>
		public long GetOffset() => this is ISwappablePointer ? 0 : ((IUnswappablePointer)this).OffsetInBytes;

		/// <summary>
		/// Make a reference <see cref="Storage{T}"/> with the same <see cref="Ptr"/> as this one while <see cref="Length"/> is changed to <paramref name="newLength"/>
		/// </summary>
		/// <param name="newLength">the new length of referenced <see cref="Storage{T}"/></param>
		/// <returns>a referenced <see cref="Storage{T}"/> (<see cref="IUnswappablePointer"/>) with different <see cref="Length"/></returns>
		public Storage<T> MakeSize(long newLength)
		{
			if (newLength == this.Length)
				return this;
			return CreateReferenceFull(this.GetRoot(), this.GetOffset(), newLength);
		}

		/// <summary>
		/// Resize this <see cref="Storage{T}"/> <b>in-place</b>, if <c><paramref name="newLength"/> &lt; <see cref="Length"/></c>, the elements with larger offsets will be removed; otherwise, some new arbitrary elements will be attached to the end.
		/// </summary>
		/// <param name="newLength">the new length to resize to</param>
		/// <exception cref="InvalidOperationException">if this is an <see cref="IUnswappablePointer"/></exception>
		public abstract void Resize(long newLength);

		/// <summary>
		/// Convert this <see cref="Storage{T}"/> to another one with different type <typeparamref name="TOut"/>
		/// </summary>
		/// <typeparam name="TOut">the output data type</typeparam>
		/// <returns>a referenced <see cref="Storage{TOut}"/> (<see cref="IUnswappablePointer"/>)</returns>
		public Storage<TOut> As<TOut>() where TOut : struct
		{
			if (this.LengthInBytes % Storage<TOut>.SizeOfT != 0)
				throw new ArgumentException(Resource.CannotDivide);
			AutoSwapMemory.NotifyUsage(this.GetRoot());
			return Storage<TOut>.CreateReferenceFull(this.GetRoot(), this.GetOffset());
		}
		#endregion

		#region equality
		/// <summary>
		/// Determines whether the specified object is equal to the current object.
		/// </summary>
		/// <param name="obj">another object</param>
		/// <returns>this equals to <paramref name="obj"/> or not</returns>
		public override bool Equals(object obj)
		{
			return this.Equals(obj as Storage<T>);
		}

		/// <summary>
		/// Determines whether the specified object is equal to the current object.
		/// </summary>
		/// <param name="obj">another object</param>
		/// <returns>this equals to <paramref name="obj"/> or not</returns>
		public bool Equals(Storage<T> obj)
		{
			if (ReferenceEquals(this, obj))
				return true;
			if (obj is null)
				return false;
			return this == obj;
		}

		/// <summary>
		/// Get the hash code of this <see cref="Storage{T}"/>
		/// </summary>
		/// <returns>the hash code</returns>
		public override int GetHashCode()
		{
			return HashCode.Combine(this.Ptr);
		}

		/// <summary>
		/// Equality operator
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns>equals or not</returns>
		public static bool operator ==(Storage<T> left, Storage<T> right)
		{
			if (left is null && right is null)
				return true;
			if ((left is null) != (right is null))
				return false;
			return left.Ptr == right.Ptr;
		}

		/// <summary>
		/// Non-equality operator
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns>not-equals or equals</returns>
		public static bool operator !=(Storage<T> left, Storage<T> right) => !(left == right);

		/// <summary>
		/// Check if this <see cref="Storage{T}"/> shares some memory with <paramref name="b"/>
		/// </summary>
		/// <param name="b">another <see cref="Storage{T}"/> to check</param>
		/// <returns>shares or not</returns>
		public bool ShareMemoryWith(Storage<T> b)
		{
			var a = this;
			if (a.OnHost != b.OnHost)
				return false;
			if (a is ISwappablePointer aa1 && b is ISwappablePointer bb1)
				return aa1.Ptr == bb1.Ptr;
			else if (a is IUnswappablePointer aa2 && b is ISwappablePointer bb2)
				return aa2.Root.Ptr == bb2.Ptr;
			else if (a is ISwappablePointer aa3 && b is IUnswappablePointer bb3)
				return aa3.Ptr == bb3.Root.Ptr;
			else if (a is IUnswappablePointer aa4 && b is IUnswappablePointer bb4)
				return aa4.Root.Ptr == bb4.Root.Ptr;
			else
				throw new NotSupportedException();
		}
		#endregion

		#region operator
		/// <summary>
		/// Add offset (in size of <typeparamref name="T"/>) to a <see cref="Storage{T}"/> to get another.
		/// </summary>
		/// <param name="storage">the pointer of type <see cref="Storage{T}"/></param>
		/// <param name="offset">the offset of type <see cref="long"/></param>
		/// <returns>a <see cref="Storage{T}"/> (<see cref="IUnswappablePointer"/>) with <paramref name="offset"/> added to the pointer</returns>
		public static Storage<T> operator +(Storage<T> storage, long offset)
		{
			if (storage is null)
				throw new ArgumentNullException(nameof(storage));

			AutoSwapMemory.NotifyUsage(storage.GetRoot());
			return CreateReference(storage, offset);
		}

		/// <summary>
		/// Subtract an offset (in size of <typeparamref name="T"/>) from a <see cref="Storage{T}"/> to get another.
		/// </summary>
		/// <param name="storage">the pointer of type <see cref="Storage{T}"/></param>
		/// <param name="offset">the offset of type <see cref="long"/></param>
		/// <returns>a <see cref="IntPtr"/> with <paramref name="offset"/> subtracted</returns>
		public static Storage<T> operator -(Storage<T> storage, long offset) => storage + (-offset);

		/// <summary>
		/// Calculate the difference of two <see cref="Storage{T}"/>s (<c><paramref name="lhs"/> - <paramref name="rhs"/></c>), in <typeparamref name="T"/> rather bytes.
		/// </summary>
		/// <param name="lhs">the left operator pointer of type <see cref="Storage{T}"/></param>
		/// <param name="rhs">the right operator pointer of type <see cref="Storage{T}"/></param>
		/// <returns>The difference as <see cref="long"/> (counted in <typeparamref name="T"/>)</returns>
		public static long operator -(Storage<T> lhs, Storage<T> rhs) => lhs - (IStorage)rhs;

		/// <summary>
		/// Calculate the difference of a <see cref="Storage{T}"/> and a <see cref="IStorage"/> (<c><paramref name="lhs"/> - <paramref name="rhs"/></c>), in <typeparamref name="T"/> rather bytes.
		/// </summary>
		/// <param name="lhs">the left operator pointer of type <see cref="Storage{T}"/></param>
		/// <param name="rhs">the right operator pointer of type <see cref="Storage{T}"/></param>
		/// <returns>The difference as <see cref="long"/> (counted in <typeparamref name="T"/>)</returns>
		public static long operator -(Storage<T> lhs, IStorage rhs)
		{
			if (lhs is null)
				throw new ArgumentNullException(nameof(lhs));
			if (rhs is null)
				throw new ArgumentNullException(nameof(rhs));

			AutoSwapMemory.NotifyUsage(lhs.GetRoot());
			AutoSwapMemory.NotifyUsage((rhs as Storage<T>)?.GetRoot() ?? (rhs as ISwappablePointer));
			long diff = lhs.Ptr.ToInt64() - rhs.Ptr.ToInt64();
			if (diff % SizeOfT != 0)
				throw new ArgumentException(Resource.CannotDivide);
			return diff / SizeOfT;
		}

		/// <summary>
		/// Calculate the difference of a <see cref="IStorage"/> and a<see cref="Storage{T}"/> (<c><paramref name="lhs"/> - <paramref name="rhs"/></c>), in <typeparamref name="T"/> rather bytes.
		/// </summary>
		/// <param name="lhs">the left operator pointer of type <see cref="Storage{T}"/></param>
		/// <param name="rhs">the right operator pointer of type <see cref="Storage{T}"/></param>
		/// <returns>The difference as <see cref="long"/> (counted in <typeparamref name="T"/>)</returns>
		public static long operator -(IStorage lhs, Storage<T> rhs) => -(rhs - lhs);

		/// <summary>
		/// Implicit convert <see cref="Storage{T}"/> to <see cref="IntPtr"/>
		/// </summary>
		/// <param name="storage">the <see cref="Storage{T}"/> to be converted</param>
		public static implicit operator IntPtr(Storage<T> storage)
		{
			if (storage is null)
				return default;
			AutoSwapMemory.NotifyUsage(storage.GetRoot());
			return storage.Ptr;
		}
		#endregion

		#region string
		// Ignore Spelling: typeof
		/// <summary>
		/// Override <see cref="object.ToString"/> to get the string representation.
		/// </summary>
		/// <returns>string representation</returns>
		public override string ToString()
		{
			return $"0x{this.ToHexString()} on {(this.OnHost ? "host" : "device")}{(this.Disposed ? " (disposed)" : "")} [type={typeof(T).Name}, length={this.Length}]";
		}

		/// <summary>
		/// Get the hex string representation of <see cref="Ptr"/> of this <see cref="Storage{T}"/>
		/// </summary>
		/// <returns>the hex string</returns>
		public string ToHexString() => this.Ptr.ToString("X");
		#endregion
	}


	#region storage factory
	/// <summary>
	/// The interface for factories of <see cref="Storage{T}"/>
	/// </summary>
	public interface IStorageFactory
	{
		/// <summary>
		/// Create a new <see cref="Storage{T}"/> and allocate memory of given size <paramref name="length"/> on memory position <paramref name="onHost"/>.
		/// </summary>
		/// <param name="length">the size in <typeparamref name="T"/> of pointer to create</param>
		/// <param name="onHost">the memory position, on host (CPU) memory or device (GPU) memory</param>
		/// <returns>the created <see cref="Storage{T}"/>, shall be a <see cref="ISwappablePointer"/></returns>
		Storage<T> CreateNew<T>(long length, bool onHost = false) where T : struct;

		/// <summary>
		/// Create a new <see cref="Storage{T}"/> with given pointer <paramref name="ptr"/>, size <paramref name="length"/> and memory position <paramref name="onHost"/>.
		/// </summary>
		/// <param name="ptr">the allocate pointer as a <see cref="IntPtr"/></param>
		/// <param name="length">the size in <typeparamref name="T"/> of pointer to create</param>
		/// <param name="onHost">the memory position, on host (CPU) memory or device (GPU) memory</param>
		/// <returns>the created <see cref="Storage{T}"/>, shall be a <see cref="ISwappablePointer"/></returns>
		Storage<T> CreateNewWith<T>(IntPtr ptr, long length, bool onHost = false) where T : struct;

		/// <summary>
		/// Create a referenced <see cref="Storage{T}"/> from a general <see cref="Storage{T}"/> as origin and <paramref name="offset"/> as offset.
		/// </summary>
		/// <param name="storage">the <see cref="Storage{T}"/> to refer to</param>
		/// <param name="offset">the offset to pointer of <paramref name="storage"/></param>
		/// <returns>the created <see cref="Storage{T}"/>, shall be a <see cref="IUnswappablePointer"/></returns>
		Storage<T> CreateReference<T>(Storage<T> storage, long offset) where T : struct;

		/// <summary>
		/// Create a referenced <see cref="Storage{T}"/> with given <paramref name="root"/>, <paramref name="offsetInBytes"/> and presenting <paramref name="length"/>.
		/// </summary>
		/// <param name="root">the root storage as an <see cref="ISwappablePointer"/></param>
		/// <param name="offsetInBytes">the offset in bytes to the pointer of <paramref name="root"/></param>
		/// <param name="length">the presenting length of created <see cref="Storage{T}"/>, negative means auto calculate</param>
		/// <returns>the created <see cref="Storage{T}"/>, shall be a <see cref="IUnswappablePointer"/></returns>
		Storage<T> CreateReferenceFull<T>(ISwappablePointer root, long offsetInBytes, long length = -1) where T : struct;
	}

	internal readonly struct DefaultStorageFactory : IStorageFactory
	{
		internal static readonly DefaultStorageFactory Singleton = new DefaultStorageFactory();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Storage<T> CreateNew<T>(long length, bool onHost = false) where T : struct
		{
			return new ActualStorage<T>(length, onHost);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Storage<T> CreateNewWith<T>(IntPtr ptr, long length, bool onHost = false) where T : struct
		{
			return new ActualStorage<T>(ptr, length, onHost);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Storage<T> CreateReference<T>(Storage<T> storage, long offset) where T : struct
		{
			return new StorageView<T>(storage, offset);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Storage<T> CreateReferenceFull<T>(ISwappablePointer root, long offsetInBytes, long length = -1) where T : struct
		{
			return new StorageView<T>(root, offsetInBytes, length);
		}
	}
	#endregion


	/// <summary>
	/// The implementation of <see cref="Storage{T}"/> and <see cref="ISwappablePointer"/>
	/// </summary>
	/// <typeparam name="T">any sequential memory layout struct</typeparam>
	internal sealed class ActualStorage<T> : Storage<T>, ISwappablePointer where T : struct
	{
		#region properties
		/// <summary>
		/// The raw pointer
		/// </summary>
		public override IntPtr Ptr { get; protected set; }

		/// <summary>
		/// The length of this pointer's underlying array in <typeparamref name="T"/>
		/// </summary>
		public override long Length { get; protected set; }

		/// <summary>
		/// This pointer is on host or device memory
		/// </summary>
		public override bool OnHost { get; protected set; }

		/// <summary>
		/// The initial location (when not swapped yet) of this pointer
		/// </summary>
		public bool DirectOnHost { get; }

		/// <summary>
		/// The time tick when this pointer was last used.
		/// </summary>
		long ISwappablePointer.LastUsedTime { get; set; } = AutoSwapMemory.timer.ElapsedTicks;
		#endregion

		#region initialize
		/// <summary>
		/// Initialize with a exist pointer.
		/// </summary>
		/// <param name="ptr">pre-allocated <see cref="IntPtr"/></param>
		/// <param name="length">length of the array</param>
		/// <param name="onHost">allocate the pointer on host or on device, default on device</param>
		public ActualStorage(IntPtr ptr, long length, bool onHost = false)
		{
			if (this.Length < 0)
				throw new ArgumentOutOfRangeException(nameof(length), length, Resource.ParaCannotNegative);
			this.Ptr = ptr;
			this.OnHost = this.DirectOnHost = onHost;
			this.Length = length;
			if (this.Length > 0)
				GC.AddMemoryPressure(this.LengthInBytes);
			AutoSwapMemory.NotifyNewStorage(this);
		}

		/// <summary>
		/// Initialize with a given length in <typeparamref name="T"/> and allocate it on device memory.
		/// </summary>
		/// <param name="length">length of the array to allocate</param>
		/// <param name="onHost">allocate the pointer on host or on device, default on device</param>
		public ActualStorage(long length, bool onHost = false)
		{
			if (length > 0)
			{
				this.Ptr = Allocate(length, onHost);
				this.OnHost = this.DirectOnHost = onHost;
				this.Length = length;
				GC.AddMemoryPressure(this.LengthInBytes);
				AutoSwapMemory.NotifyNewStorage(this);
			}
			else
			{
				this.Ptr = IntPtr.Zero;
			}
		}

		/// <summary>
		/// Allocate memory on host or device.
		/// </summary>
		/// <param name="length">memory length in <typeparamref name="T"/> to allocate</param>
		/// <param name="onHost">allocate the pointer on host or on device</param>
		/// <returns>allocated address as <see cref="IntPtr"/></returns>
		/// <exception cref="OverflowException">if the <paramref name="length"/> * <see cref="Storage{T}.SizeOfT"/> triggers overflow</exception>
		/// <exception cref="InsufficientMemoryException">if this size of memory cannot be allocated anymore</exception>
		/// <exception cref="AccessViolationException">if the memory cannot be allocated because of other reasons</exception>
		private static IntPtr Allocate(long length, bool onHost)
		{
			var pointer = new IntPtr();
			if (length <= 0)
				return pointer;
			long count;
			checked // check overflow for the first time
			{
				count = SizeOfT * length;
			}

			string locationInfo = $"Cannot allocate {(SizeOfT * length / 1024.0 / 1024.0):N1}MiB memory on " + (onHost ? "host" : ("GPU" + RT.DeviceNo));
			if (onHost)
			{
				pointer = Runtime.Mkl.NativeMethods.MKL_malloc(count, 256);
				
				if (pointer == IntPtr.Zero)
				{
					GC.Collect();
					GC.WaitForPendingFinalizers();
					pointer = Runtime.Mkl.NativeMethods.MKL_malloc(count, 256);
					if (pointer == IntPtr.Zero)
						throw new AccessViolationException($"{locationInfo} because of other error.");
					else
						Log.Write($"Out of memory detected, GC is performed.", category: "Array allocation", level: LogLevel.Error);
				}
			}
			else
			{
				CudaError err = Runtime.Cuda.NativeMethods.cudaMalloc(ref pointer, count);
				if (err != CudaError.Success || pointer == IntPtr.Zero)
				{
					Exception exception = err == CudaError.ErrorOutOfMemory ?
						new InsufficientMemoryException($"{locationInfo} since you have ran out of memory.") :
						(Exception)new AccessViolationException($"{locationInfo} because of other error '{err}'.");
					if (!Settings.AutoGCWhenOutOfMemory || !(exception is InsufficientMemoryException))
						throw exception;

					GC.Collect();
					GC.WaitForPendingFinalizers();
					AutoSwapMemory.NotifyNewDeviceMemory(count);
					err = Runtime.Cuda.NativeMethods.cudaMalloc(ref pointer, count);
					if (err != CudaError.Success)
						throw exception;
					else
						Log.Write($"Out of memory detected, GC is performed.", category: "Array allocation", level: LogLevel.Error);
				}
			}
			return pointer;
		}
		#endregion

		#region dispose
		/// <summary>
		/// Finalizer
		/// </summary>
		~ActualStorage() => this.Dispose(false);

		/// <summary>
		/// The actual dispose method
		/// </summary>
		/// <param name="disposeManaged">dispose managed resource or not</param>
		protected override void Dispose(bool disposeManaged)
		{
			if (!this.Disposed && this.Ptr != default && this.Length > 0)
			{
				if (this.OnHost)
					Runtime.Mkl.NativeMethods.MKL_free(this.Ptr);
				else
				{
					var err = Runtime.Cuda.NativeMethods.cudaFree(this.Ptr);
					if (err != CudaError.Success)
						Log.Write(string.Format(Resource.Culture, Resource.FreeFail, "GPU" + RT.DeviceNo, this.Ptr.ToInt64()), level: LogLevel.Error);
				}
				AutoSwapMemory.NotifyDisposeStorage(this);
				GC.RemoveMemoryPressure(this.LengthInBytes);
			}
			this.Disposed = true;
			GC.SuppressFinalize(this);
		}
		#endregion

		#region override
		/// <summary>
		/// <b>In-place</b> move this storage to the other memory, only works for <see cref="ActualStorage{T}"/>
		/// </summary>
		public override void ToOtherMemory()
		{
			// create temp and copy
			using var tempStorate = new ActualStorage<T>(this.Length, !this.OnHost); // will not be disposed
			RT.CopyTo(source: this, dest: tempStorate, this.Length);
			// release original unmanaged resources of this one
			this.Dispose();
			// re-register disposition of this one
			this.Disposed = false;
			GC.ReRegisterForFinalize(this);
			// change pointer of this one
			this.Ptr = tempStorate.Ptr;
			this.OnHost = !this.OnHost;
			// make sure temp will not be disposed
			tempStorate.Disposed = true;
			GC.SuppressFinalize(tempStorate);
		}

		/// <summary>
		/// Resize this <see cref="Storage{T}"/> <b>in-place</b>
		/// </summary>
		/// <param name="newLength">the new length to resize to</param>
		public override void Resize(long newLength)
		{
			AutoSwapMemory.NotifyUsage(this);
			// create temp and copy
			using var tempStorate = new ActualStorage<T>(newLength, this.OnHost);
			RT.CopyTo(source: this, dest: tempStorate, Math.Min(newLength, this.Length));
			// release original unmanaged resources of this one
			this.Dispose();
			// re-register disposition of this one
			this.Disposed = false;
			GC.ReRegisterForFinalize(this);
			// change pointer of this one
			this.Ptr = tempStorate.Ptr;
			this.Length = newLength;
			// make sure temp will not be disposed
			tempStorate.Disposed = true;
			GC.SuppressFinalize(tempStorate);
		}
		#endregion
	}

	/// <summary>
	/// The reference implementation of <see cref="Storage{T}"/> and <see cref="IUnswappablePointer"/>
	/// </summary>
	/// <typeparam name="T">any sequential memory layout struct</typeparam>
	internal sealed class StorageView<T> : Storage<T>, IUnswappablePointer where T : struct
	{
		#region properties
		/// <summary>
		/// The root pointer, i.e. the pointer of interface <see cref="ISwappablePointer"/> that directly contains unmanaged resources.
		/// </summary>
		public ISwappablePointer Root { get; }

		/// <summary>
		/// The offset of this pointer compared to <see cref="Root"/> in bytes.
		/// </summary>
		public long OffsetInBytes { get; }

		/// <summary>
		/// The raw pointer
		/// </summary>
		public override IntPtr Ptr {
			get => new IntPtr(this.Root.Ptr.ToInt64() + this.OffsetInBytes);
			protected set => throw new InvalidOperationException();
		}

		/// <summary>
		/// The length of this pointer's underlying array in <typeparamref name="T"/>
		/// </summary>
		public override long Length { get; protected set; }

		/// <summary>
		/// This pointer is on host or device memory
		/// </summary>
		public override bool OnHost {
			get => this.Root.OnHost;
			protected set => throw new InvalidOperationException();
		}

		#endregion

		#region initialize
		/// <summary>
		/// Create a new <see cref="StorageView{T}"/> from a general <see cref="Storage{T}"/> and its <paramref name="offset"/>
		/// </summary>
		/// <param name="storage">the <see cref="Storage{T}"/> to refer to</param>
		/// <param name="offset">the offset to pointer of <paramref name="storage"/></param>
		public StorageView(Storage<T> storage, long offset)
		{
			if (storage is null || storage.Ptr == IntPtr.Zero)
				return;
			var root = storage.GetRoot();
			long totalOffset = (storage - root) + offset;
			long lengthRoot = root.LengthInBytes / SizeOfT;
			if (totalOffset >= lengthRoot || totalOffset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset));
			this.Root = root;
			this.OffsetInBytes = totalOffset * SizeOfT;
			this.Length = lengthRoot - totalOffset;
		}

		/// <summary>
		/// Full constructor
		/// </summary>
		public StorageView(ISwappablePointer root, long offsetInBytes, long length = -1)
		{
			this.Root = root;
			this.OffsetInBytes = offsetInBytes;
			this.Length = length < 0 ? (root.LengthInBytes - offsetInBytes) / SizeOfT : length;
		}
		#endregion

		#region dispose
		/// <summary>
		/// The actual dispose method
		/// </summary>
		/// <param name="disposeManaged">dispose managed resource or not</param>
		protected override void Dispose(bool disposeManaged)
		{
			// do nothing
		}
		#endregion

		#region override
		/// <summary>
		/// <b>In-place</b> move this storage to the other memory, only works for <see cref="ActualStorage{T}"/>
		/// </summary>
		public override void ToOtherMemory()
		{
			throw new InvalidOperationException();
		}

		/// <summary>
		/// Resize this <see cref="Storage{T}"/> <b>in-place</b>
		/// </summary>
		/// <param name="newLength">the new length to resize to</param>
		public override void Resize(long newLength)
		{
			throw new InvalidOperationException();
		}
		#endregion
	}


	/// <summary>
	/// Some extension methods of list of arrays
	/// </summary>
	public static class ListOfArraysExtension
	{
		/// <summary>
		/// Clear a general array
		/// </summary>
		/// <typeparam name="TArr">the array type</typeparam>
		/// <param name="array">the array to clear</param>
		public static void ClearList<TArr>(this TArr[] array) where TArr : IDisposable
		{
			if (array is null)
				return;
			array.ForEach(l => l?.Dispose());
			Array.Clear(array, 0, array.Length);
		}

		/// <summary>
		/// Clear a general list
		/// </summary>
		/// <typeparam name="TArr">the array type</typeparam>
		/// <param name="list">the list to clear</param>
		public static void ClearList<TArr>(this List<TArr> list) where TArr : IDisposable
		{
			if (list is null)
				return;
			list.ForEach(l => l?.Dispose());
			list.Clear();
		}

		/// <summary>
		/// Dispose a general read-only list
		/// </summary>
		/// <typeparam name="TArr">the array type</typeparam>
		/// <param name="list">the read-only list to dispose</param>
		public static void ClearList<TArr>(this IReadOnlyList<TArr> list) where TArr : IDisposable
		{
			if (list is null)
				return;
			for (int i = 0; i < list.Count; i++)
			{
				list[i]?.Dispose();
			}
		}

		/// <summary>
		/// Dispose a general dictionary
		/// </summary>
		/// <typeparam name="T">the dictionary key type</typeparam>
		/// <typeparam name="TArr">the array type</typeparam>
		/// <param name="dict">the dictionary to dispose</param>
		public static void ClearDict<T, TArr>(this IReadOnlyDictionary<T, TArr> dict) where TArr : IDisposable
		{
			if (dict is null)
				return;
			foreach (var item in dict)
			{
				item.Value?.Dispose();
			}
		}
	}

	internal static class AutoSwapMemory
	{
		internal static readonly Stopwatch timer = Stopwatch.StartNew();

		private static readonly List<ISwappablePointer> devicePointers = new List<ISwappablePointer>(), swappedPointers = new List<ISwappablePointer>();

		internal static void NotifyNewStorage(ISwappablePointer pointer)
		{
			if (!pointer.DirectOnHost)
				devicePointers.Add(pointer);
		}

		internal static void NotifyDisposeStorage(ISwappablePointer pointer)
		{
			if (!pointer.DirectOnHost)
				devicePointers.Remove(pointer);
			else
				swappedPointers.Remove(pointer);
		}

		internal static void NotifyUsage(ISwappablePointer pointer)
		{
			if (pointer is null)
				return;
			pointer.LastUsedTime = timer.ElapsedTicks;
			if (pointer.DirectOnHost && swappedPointers.Contains(pointer))
			{
				bool success = NotifyNewDeviceMemory(pointer.LengthInBytes);
				if (!success)
				{
					GC.Collect(); GC.WaitForPendingFinalizers();
					success = NotifyNewDeviceMemory(pointer.LengthInBytes);
				}
				if (!success) // still fails to make enough room
					throw new InsufficientMemoryException($"Cannot swap {(pointer.LengthInBytes / 1024.0 / 1024.0):N1}MiB memory back to device.");
				pointer.ToOtherMemory();
			}
		}

		internal static bool NotifyNewDeviceMemory(long lenghtInBytes)
		{
			var (free, total) = RT.DeviceFreeAndTotalMemory;
			if (free + lenghtInBytes < total)
				return true;
			long lengthNeed = lenghtInBytes - free;
			var ordered = devicePointers.OrderBy(p => p.LastUsedTime);
			long len = 0; int i;
			for (i = 0; i < ordered.Count; i++)
			{
				len += ordered[i].LengthInBytes;
				if (len >= lengthNeed)
					break;
			}
			if (i + 1 == ordered.Count)
				return false;
			for (int j = 0; j <= i; j++)
			{
				ordered[j].ToOtherMemory();
				swappedPointers.Add(ordered[j]);
				devicePointers.Remove(ordered[j]);
			}
			return true;
		}
	}
}
