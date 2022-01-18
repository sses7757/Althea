using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using Althea.Helpers;
using Althea.Linq;
using Althea.NativeTypes;
using Althea.Resources;

using MEM = Althea.Storage.AbstractApi;


namespace Althea.Storage
{
	internal delegate IStorage CreateDelegate(CombinationType type, ReadOnlySpan<StorageLocation> locations, ReadOnlySpan<long> lengths);

	/// <summary>
	/// The storage factory for creating concrete storage classes. This is a simple factory pattern.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct as the data type</typeparam>
	public static class StorageFactory<T> where T : unmanaged
	{
		#region basic
		/// <summary>
		/// Encapsulates a method that allocates and creates a new <see cref="Storage{T}"/> with given <paramref name="locations"/> and <paramref name="lengths"/>
		/// </summary>
		/// <param name="locations">The given <see cref="ReadOnlySpan{T}"/> of <see cref="StorageLocation"/>s indicating the locations</param>
		/// <param name="lengths">The given <see cref="ReadOnlySpan{T}"/> of <see cref="long"/> indicating the length in <typeparamref name="T"/> of each location</param>
		/// <returns>The created new <see cref="Storage{T}"/></returns>
		/// <remarks>Independent checks for parameters are not necessary</remarks>
		public delegate ActualStorage<T> CreateDelegate(ReadOnlySpan<StorageLocation> locations, ReadOnlySpan<long> lengths);


		private static readonly Dictionary<CombinationType, CreateDelegate> cache_create = new()
		{
			[CombinationType.PureOrMixed] = DefaultCreatePureOrMixed,
			[CombinationType.Cached] = DefaultCreateCached,
		};

		private static ActualStorage<T> DefaultCreatePureOrMixed(ReadOnlySpan<StorageLocation> locations, ReadOnlySpan<long> lengths)
		{
			if (locations.Length == 1)
				return new PureStorage<T>(locations[0], lengths[0]);
			else
				return new MixedStorage<T>(locations, lengths);
		}

		private static ActualStorage<T> DefaultCreateCached(ReadOnlySpan<StorageLocation> locations, ReadOnlySpan<long> lengths)
		{
			if (locations.Length == 2 &&
				locations[0].Type.GetClassification() == LocationTypeExtension.ClassMemory &&
				locations[1].Type.GetClassification() == LocationTypeExtension.ClassStream)
				return new StreamToMemoryCachedStorage<T>(locations[0], locations[1], lengths[0], lengths[1]);
			else
				throw new InvalidOperationException();
		}

		/// <summary>
		/// Set the creation method for a given <see cref="CombinationType"/>
		/// </summary>
		/// <param name="combinationType">The given <see cref="CombinationType"/> to set the creation method</param>
		/// <param name="createDelegate">The creation method as a <see cref="CreateDelegate"/></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void SetCreateMethod(CombinationType combinationType, CreateDelegate createDelegate)
		{
			cache_create[combinationType] = createDelegate;
		}


		private delegate bool _SupportDelegate(CombinationType combinationType, ReadOnlySpan<StorageLocation> locations);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool TryFindAndSetCreateMethod(CombinationType combinationType, ReadOnlySpan<StorageLocation> locations)
		{
			var thisAssembly = Assembly.GetExecutingAssembly();
			var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a != thisAssembly);
			static IReadOnlyList<Type> GetTypes(Assembly a)
			{
				try
				{
					return a.GetExportedTypes();
				}
				catch (System.Exception)
				{
					return Array.Empty<Type>();
				}
			}
			var types = assemblies.SelectMany(GetTypes);
			Type actualStorageType = typeof(ActualStorage<T>), referenceStorageType = typeof(IReferenceStorage);
			Type[] supportMethodArgTypes = new[] { typeof(CombinationType), typeof(ReadOnlySpan<StorageLocation>) };
			Type[] constructorArgTypes = new[] { typeof(ReadOnlySpan<StorageLocation>), typeof(ReadOnlySpan<long>) };
			for (int i = 0; i < types.Count; i++)
			{
				var type = types[i];
				if (!type.IsGenericType || type.GenericTypeArguments.Length != 1 || type.IsAbstract || type.IsInterface || !type.IsClass)
					continue;
				try
				{
					type = type.MakeGenericType(typeof(T));
				}
				catch (System.Exception)
				{
					continue;
				}
				if (!type.IsAssignableTo(actualStorageType) || type.IsAssignableTo(referenceStorageType))
					continue;
				try
				{
					var method = type.GetMethod(nameof(PureStorageBase<T>.IsSupported), BindingFlags.Public | BindingFlags.Static, null, supportMethodArgTypes, null);
					if (method is null)
						continue;
					if (!method.CreateDelegate<_SupportDelegate>().Invoke(combinationType, locations))
						continue;
				}
				catch (System.Exception)
				{
					continue;
				}
				try
				{
					var ctor = type.GetConstructor(constructorArgTypes);
					if (ctor is null)
						continue;
					DynamicMethod dynamic = new(string.Empty, type, constructorArgTypes, type);
					ILGenerator il = dynamic.GetILGenerator();
					il.DeclareLocal(type);
					il.Emit(OpCodes.Newobj, ctor);
					il.Emit(OpCodes.Stloc_0);
					il.Emit(OpCodes.Ldloc_0);
					il.Emit(OpCodes.Ret);
					var func = (CreateDelegate)dynamic.CreateDelegate(typeof(CreateDelegate));
					if (func is null)
						continue;
					// success
					cache_create.Add(combinationType, func);
					return true;
				}
				catch (System.Exception)
				{
					continue;
				}
			}
			// cannot find any
			return false;
		}
		#endregion

		#region create
		/// <summary>
		/// Allocate and create a new <see cref="Storage{T}"/> with given <paramref name="locations"/> and <paramref name="lengths"/>
		/// </summary>
		/// <param name="type">The <see cref="CombinationType"/> used to identify which creation method to use</param>
		/// <param name="locations">The given <see cref="ReadOnlySpan{T}"/> of <see cref="StorageLocation"/> indicating the locations</param>
		/// <param name="lengths">The given <see cref="ReadOnlySpan{T}"/> of <see cref="long"/> indicating the length in <typeparamref name="T"/> of each location</param>
		/// <returns>The created new <see cref="Storage{T}"/></returns>
		/// <remarks>If the creation method of <paramref name="type"/> is neither default indicated nor manually indicated by <see cref="SetCreateMethod"/>,<br/>
		/// this method will try to find the first suitable one by iterating all exported types of all loaded assemblies,<br/>
		/// (the type with static method like <see cref="PureStorageBase{T}.IsSupported"/> and constructor using <paramref name="locations"/> and <paramref name="lengths"/>)<br/>
		/// which can be <b>really</b> slow. Therefore, try to use <see cref="SetCreateMethod"/> before calling this method if possible.</remarks>
		/// <exception cref="InvalidOperationException">If the creation method of <paramref name="type"/> is neither default indicated nor manually indicated by <see cref="SetCreateMethod(CombinationType, CreateDelegate)"/>, and it cannot be obtained from the public constructors of other assemblies</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ActualStorage<T> Create(CombinationType type, ReadOnlySpan<StorageLocation> locations, ReadOnlySpan<long> lengths)
		{
			if (!cache_create.ContainsKey(type))
				throw new InvalidOperationException();

			if (!cache_create.ContainsKey(type))
			{
				if (!TryFindAndSetCreateMethod(type, locations))
					throw new InvalidOperationException();
			}
			return cache_create[type].Invoke(locations, lengths);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void GetLocationsAndLengths(Storage<T> storage, Span<StorageLocation> locations, Span<long> lengths)
		{
			int sizeT = Const<T>.SizeT;
			var descr = storage.LocationDescription;
			descr.CopyLocationsToSpan(locations);
			// get lengths
			// the most possible case
			if (storage.LengthInBytes == storage.Sum(static p => p.LengthInBytes))
			{
				long bytesLeft = 0;
				for (int i = 0; i < descr.Count; i++)
				{
					long lengthInBytes = bytesLeft + storage[i].LengthInBytes;
					lengths[i] = lengthInBytes / sizeT;
					bytesLeft = lengthInBytes - lengths[i] * sizeT;
				}
				return;
			}
			// else, less possible, need GetActualPointerAt()
			long actualOccupiedBytes = 0;
			for (int i = 0; i < descr.Count; i++)
				actualOccupiedBytes += storage.GetActualPointerAt(i).LengthInBytes;
			if (storage.LengthInBytes == actualOccupiedBytes)
			{
				long bytesLeft = 0;
				for (int i = 0; i < descr.Count; i++)
				{
					long lengthInBytes = bytesLeft + storage.GetActualPointerAt(i).LengthInBytes;
					lengths[i] = lengthInBytes / sizeT;
					bytesLeft = lengthInBytes - lengths[i] * sizeT;
				}
				return;
			}
			// else, cannot auto align
			for (int i = 0; i < descr.Count; i++)
			{
				long length = storage.GetActualPointerAt(i).LengthInBytes;
				if (length % sizeT != 0)
					throw new ArgumentException(Other.CannotDivide, nameof(storage));
				lengths[i] = length / sizeT;
			}
		}

		/// <summary>
		/// Allocate and create a new <see cref="Storage{T}"/> alike the given <paramref name="storage"/>
		/// </summary>
		/// <param name="storage">The given <see cref="Storage{T}"/> as the template of <see cref="Storage{T}.LocationDescription"/> and lengths to create the new one</param>
		/// <returns>A new <see cref="Storage{T}"/> alike <paramref name="storage"/></returns>
		/// <exception cref="ArgumentException">If <paramref name="storage"/>'s <see cref="PointerSegment"/> are not aligned to the size of <typeparamref name="T"/>, meanwhile auto alignment cannot be done with neither <see cref="Storage{T}.this[int]"/> nor <see cref="Storage{T}.GetActualPointerAt(int)"/></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ActualStorage<T> CreateAlike(Storage<T> storage)
		{
			int sizeT = Const<T>.SizeT;
			var descr = storage.LocationDescription;
			Span<StorageLocation> locations = stackalloc StorageLocation[descr.Count];
			Span<long> lengths = stackalloc long[descr.Count];
			GetLocationsAndLengths(storage, locations, lengths);
			return Create(descr.Type, locations, lengths);
		}

		/// <summary>
		/// Allocate and create a new <see cref="Storage{T}"/> alike the given <paramref name="storage"/>
		/// </summary>
		/// <param name="storage">The given <see cref="Storage{T}"/> as the template of <see cref="Storage{T}.LocationDescription"/> and lengths to create the new one</param>
		/// <returns>A new <see cref="Storage{T}"/> of type <typeparamref name="TOut"/> alike <paramref name="storage"/> with the new lengths in <typeparamref name="TOut"/> equals to the original lengths in <typeparamref name="T"/></returns>
		/// <exception cref="ArgumentException">If <paramref name="storage"/>'s <see cref="PointerSegment"/> are not aligned to the size of <typeparamref name="T"/>, meanwhile auto alignment cannot be done with neither <see cref="Storage{T}.this[int]"/> nor <see cref="Storage{T}.GetActualPointerAt(int)"/></exception>
		public static ActualStorage<TOut> CreateAlike<TOut>(Storage<T> storage) where TOut : unmanaged
		{
			// shortcut
			if (storage is Storage<TOut> s)
				return s.CreateAlike();
			// otherwise
			int sizeT = Const<T>.SizeT;
			var descr = storage.LocationDescription;
			Span<StorageLocation> locations = stackalloc StorageLocation[descr.Count];
			Span<long> lengths = stackalloc long[descr.Count];
			GetLocationsAndLengths(storage, locations, lengths);
			return StorageFactory<TOut>.Create(descr.Type, locations, lengths);
		}
		#endregion
	}
}
