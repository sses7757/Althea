using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;


namespace Althea.Arrays
{
	/// <summary>
	/// The interface for array factory
	/// </summary>
	public interface IArrayFactory
	{
		/// <summary>
		/// When implemented by a derived factory class, reconstruct a <see cref="ValueArray{T}"/> of the derived factory's corresponding array type using <paramref name="size"/>, <paramref name="storages"/> as well as other information obtained from <see cref="ValueArray{T}.GetMetaData"/>.
		/// </summary>
		/// <typeparam name="T">The data type</typeparam>
		/// <param name="size">The size of the <see cref="ValueArray{T}"/> about to create</param>
		/// <param name="storages">All the storage(s) of the array(s) of the <see cref="ValueArray{T}"/> about to create, one of which must be <see cref="ValueArray{T}.StorageName"/></param>
		/// <param name="otherInfo">other information obtained from <see cref="ValueArray{T}.GetMetaData"/></param>
		/// <returns>The reconstructed <see cref="ValueArray{T}"/> of the derived factory's corresponding array type</returns>
		/// <exception cref="ArgumentException">If the any of the arguments is invalid (such as <paramref name="storages"/> not containing <see cref="ValueArray{T}.StorageName"/>)</exception>
		ValueArray<T> CreateArray<T>(ReadOnlySpan<long> size, IReadOnlyDictionary<string, IStorage> storages, IReadOnlyDictionary<string, object>? otherInfo = null) where T : unmanaged, IFormattable, IEquatable<T>;
	}

	/// <summary>
	/// The <see cref="ValueArray{T}"/> factory used to create instances
	/// </summary>
	/// <typeparam name="T">An unmanaged struct as the data type</typeparam>
	public static class ValueArrayFactory<T> where T : unmanaged, IFormattable, IEquatable<T>
	{
		#region private helper
		internal delegate ValueArray<T> DelegateCreateArray(ReadOnlySpan<long> size, IReadOnlyDictionary<string, IStorage> storages, IReadOnlyDictionary<string, object>? otherInfo = null);

		private static readonly Dictionary<RuntimeTypeHandle, DelegateCreateArray> static_cache = new();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static DelegateCreateArray GetDelegate(Type type)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));
			// cache
			var key = type.TypeHandle;
			DelegateCreateArray? delegateCreate;
			if (static_cache.ContainsKey(key))
			{
				delegateCreate = static_cache[key];
				if (delegateCreate is not null)
					return delegateCreate;
			}
			// else, cache miss
			if (!type.IsAssignableTo(typeof(ValueArray<T>)) || type.IsAbstract)
				throw new TypeAccessException();
			string fullName = type.AssemblyQualifiedName ?? throw new ArgumentException(Resources.Parameter.UnexpectedType, nameof(type));
			int find = fullName.IndexOf('`');
			if (find >= 0)
				fullName = fullName.Substring(0, find);
			fullName += "Factory"; // the factory name
			var factoryType = Type.GetType(fullName);
			if (!typeof(IArrayFactory).IsAssignableFrom(factoryType))
				throw new TypeAccessException();
			var factoryInstance = Activator.CreateInstance(factoryType) ?? throw new TypeInitializationException(factoryType.FullName, null);
			var method = factoryType.GetMethod(nameof(IArrayFactory.CreateArray)) ?? throw new MissingMethodException();
			method = method.MakeGenericMethod(typeof(T));
			delegateCreate = method.CreateDelegate<DelegateCreateArray>(factoryInstance);
			// add to cache
			static_cache.Add(key, delegateCreate);
			return delegateCreate;
		}
		#endregion

		#region create
		/// <summary>
		/// Create a <see cref="ValueArray{T}"/> of concrete type <typeparamref name="TArray"/> using the factory pattern.
		/// </summary>
		/// <typeparam name="TArray">The concrete array type, must be a <see cref="ValueArray{T}"/></typeparam>
		/// <param name="size">The size of the <see cref="ValueArray{T}"/> about to create</param>
		/// <param name="storages">All the storage(s) of the array(s) of the <see cref="ValueArray{T}"/> about to create, one of which must be <see cref="ValueArray{T}.StorageName"/></param>
		/// <param name="otherInfo">other information obtained from <see cref="ValueArray{T}.GetMetaData"/></param>
		/// <returns>The reconstructed <see cref="ValueArray{T}"/> of the type <typeparamref name="TArray"/></returns>
		/// <remarks>If <typeparamref name="TArray"/> is a user-defined class that inherits <see cref="ValueArray{T}"/>, its factory must also be created with the same class name and a postfix "Factory" in the same naming space.</remarks>
		/// <exception cref="TypeAccessException">If <typeparamref name="TArray"/>'s factory is not a sub-class of <see cref="IArrayFactory"/></exception>
		/// <exception cref="TypeLoadException">If <typeparamref name="TArray"/>'s factory cannot be loaded</exception>
		/// <exception cref="TypeInitializationException">If <typeparamref name="TArray"/>'s factory cannot be created with parameterless constructor</exception>
		/// <exception cref="System.Reflection.AmbiguousMatchException">If <typeparamref name="TArray"/>'s factory has multiple method named <see cref="IArrayFactory.CreateArray"/></exception>
		/// <exception cref="MissingMethodException">If <typeparamref name="TArray"/>'s factory has no method <see cref="IArrayFactory.CreateArray"/></exception>
		public static TArray Create<TArray>(ReadOnlySpan<long> size, IReadOnlyDictionary<string, IStorage> storages, IReadOnlyDictionary<string, object>? otherInfo = null)
			where TArray : ValueArray<T>, new()
		{
			var delegateCreate = GetDelegate(typeof(TArray));
			return (TArray)delegateCreate(size, storages, otherInfo);
		}

		/// <summary>
		/// Create a <see cref="ValueArray{T}"/> of concrete <paramref name="type"/> using the factory pattern.
		/// </summary>
		/// <param name="type">The concrete array type to create</param>
		/// <param name="size">The size of the <see cref="ValueArray{T}"/> about to create</param>
		/// <param name="storages">All the storage(s) of the array(s) of the <see cref="ValueArray{T}"/> about to create, one of which must be <see cref="ValueArray{T}.StorageName"/></param>
		/// <param name="otherInfo">other information obtained from <see cref="ValueArray{T}.GetMetaData"/></param>
		/// <returns>The reconstructed <see cref="ValueArray{T}"/> of <paramref name="type"/></returns>
		/// <remarks>If <paramref name="type"/> is a user-defined class that inherits <see cref="ValueArray{T}"/>, its factory must also be created with the same class name and a postfix "Factory" in the same naming space.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="type"/> is null</exception>
		/// <exception cref="TypeAccessException">If <paramref name="type"/>'s factory is not a sub-class of <see cref="IArrayFactory"/></exception>
		/// <exception cref="TypeLoadException">If <paramref name="type"/>'s factory cannot be loaded</exception>
		/// <exception cref="TypeInitializationException">If <paramref name="type"/>'s factory cannot be created with parameterless constructor</exception>
		/// <exception cref="System.Reflection.AmbiguousMatchException">If <paramref name="type"/>'s factory has multiple method named <see cref="IArrayFactory.CreateArray"/></exception>
		/// <exception cref="MissingMethodException">If <paramref name="type"/>'s factory has no method <see cref="IArrayFactory.CreateArray"/></exception>
		public static ValueArray<T> Create(Type type, ReadOnlySpan<long> size, IReadOnlyDictionary<string, IStorage> storages, IReadOnlyDictionary<string, object>? otherInfo = null)
		{
			var delegateCreate = GetDelegate(type);
			return delegateCreate(size, storages, otherInfo);
		}
		#endregion

		#region public helper
		/// <summary>
		/// Check the value array storage in the <paramref name="storages"/> and cast it into type <typeparamref name="T"/>
		/// </summary>
		/// <param name="storages">The dictionary of the storages</param>
		/// <param name="size">The size to check, default 0 means do not check</param>
		/// <returns>The casted <see cref="Storage{T}"/> of the value array</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="size"/> is less than 0</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="storages"/> is null or it does not contain <see cref="ValueArray{T}.StorageName"/></exception>
		/// <exception cref="ArgumentException">If <paramref name="storages"/>[<see cref="ValueArray{T}.StorageName"/>] is not a <see cref="Storage{T}"/></exception>
		public static Storage<T> CheckValueStorage(IReadOnlyDictionary<string, IStorage> storages, long size = 0)
			=> CheckStorage<T>(storages, ValueArray<T>.StorageName, size);

		/// <summary>
		/// Check storage named <paramref name="name"/> in the <paramref name="storages"/> and cast it into type <typeparamref name="U"/>
		/// </summary>
		/// <typeparam name="U">Any unmanaged struct as the data type</typeparam>
		/// <param name="storages">The dictionary of the storages</param>
		/// <param name="name">The name to get</param>
		/// <param name="size">The size to check, default 0 means do not check</param>
		/// <returns>The casted <see cref="Storage{T}"/> of the value array</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="size"/> is less than 0</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="storages"/> is null or it does not contain <paramref name="name"/></exception>
		/// <exception cref="ArgumentException">If <paramref name="storages"/>[<paramref name="name"/>] is not a <see cref="Storage{T}"/></exception>
		public static Storage<U> CheckStorage<U>(IReadOnlyDictionary<string, IStorage> storages, string name, long size = 0) where U : unmanaged
		{
			if (storages is null || !storages.ContainsKey(name))
				throw new ArgumentNullException(nameof(storages));
			if (size < 0)
				throw new ArgumentOutOfRangeException(nameof(size));

			Storage<U>? data = null;
			if (storages[name] is ReferenceStorage<byte> pb)
			{
				if (pb.TotalOffsetInBytes == 0 && pb.Reference is Storage<U> pt && pb.LengthInBytes == pt.LengthInBytes)
					data = pt;
			}
			else if (storages[name] is Storage<U> pt)
			{
				data = pt;
			}
			if (data is null || !data.IsValid())
				throw new ArgumentException(Resources.Parameter.UnexpectedType, nameof(storages));
			if (size > 0)
			{
				if (data.Length != size)
					throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(storages));
			}
			return data;
		}
		#endregion
	}
}
