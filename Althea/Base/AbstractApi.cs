using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace Althea
{
	/// <summary>
	/// The base abstract interface for all runtime API classes defined in and out of this assembly. Any concrete runtime API class shall inherit the sub-classes of this class.<br/>
	/// The derived concrete class(es) shall be able to serialized and deserialized by <see cref="JsonSerializer"/> (typically through indicating <see cref="JsonConstructorAttribute"/> and implementing <see cref="CurrentConverter"/>).
	/// </summary>
	/// <typeparam name="TApi">Any runtime API abstract class which inherits <see cref="IAbstractRuntimeApi{TApi}"/></typeparam>
	/// <remarks>Typically, the <b>callers</b> are responsible for checking the input parameters of all the defined API methods.</remarks>
	public interface IAbstractRuntimeApi<TApi> : IDisposable where TApi : IAbstractRuntimeApi<TApi>
	{
		#region basic
		/// <summary>
		/// When implemented by a derived class, get a <see cref="bool"/> indicating whether this instance is disposed or not
		/// </summary>
		public bool Disposed { get; protected set; }

		/// <summary>
		/// When implemented by a derived class, get the <see cref="JsonConverter{T}"/> used to serialize it (deserialization is done via default of <see cref="JsonSerializer"/> or <see cref="JsonConstructorAttribute"/>). The default implementation simply returns null.
		/// </summary>
		protected internal virtual JsonConverter<TApi>? CurrentConverter => null;

		private delegate TApi CreateDelegate();

		private static readonly Dictionary<Type, CreateDelegate> CreateDelegates = new();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static TApi Create<TImpl>() where TImpl : TApi, new() => new TImpl();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static TApi Create(Type type)
		{
			if (!CreateDelegates.ContainsKey(type))
			{
				var method = typeof(IAbstractRuntimeApi<TApi>).GetMethod(nameof(Create), 1, BindingFlags.NonPublic | BindingFlags.Static, null, Array.Empty<Type>(), null)?.MakeGenericMethod(type);
				if (method is null)
					throw new ArgumentException(Resources.ParameterError.UnexpectedType, nameof(type));
				CreateDelegates[type] = method.CreateDelegate<CreateDelegate>();
			}
			try
			{
				return CreateDelegates[type].Invoke();
			}
			catch (Exception e)
			{
				throw new InvalidOperationException(Resources.BackendError.CannotInitialize, e);
			}
		}
		#endregion
	}

	#region API manager
	internal static class ApiManager
	{
		private static readonly Dictionary<object, List<RuntimeTypeHandle>> InstanceApis = new();

		private static readonly object __locker = new();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int Count<TApi>() where TApi : IAbstractRuntimeApi<TApi>
		{
			return AbstractApiSelector<TApi>.APIs.Count;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static TApi Get<TApi>(int index) where TApi : IAbstractRuntimeApi<TApi>
		{
			return AbstractApiSelector<TApi>.APIs[index];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int IndexOf<TApi>(TApi instance) where TApi : IAbstractRuntimeApi<TApi>
		{
			return AbstractApiSelector<TApi>.APIs.IndexOf(instance);
		}

		internal static void Add<TApi>(TApi instance) where TApi : IAbstractRuntimeApi<TApi>
		{
			var handle = typeof(TApi).TypeHandle;
			lock (__locker)
			{
				AbstractApiSelector<TApi>.APIs.Add(instance);
				if (InstanceApis.TryGetValue(instance, out var list))
					list.Add(handle);
				else
					InstanceApis[instance] = new() { handle };
			}
		}

		internal static void Dispose<TApi>(int index) where TApi : IAbstractRuntimeApi<TApi>
		{
			lock (__locker)
			{
				var instance = AbstractApiSelector<TApi>.APIs[index];
				var apis = InstanceApis[instance];
				if (apis.Count == 1)
				{
					instance.Dispose();
				}
				else
				{
					var handle = typeof(TApi).TypeHandle;
					int find = apis.IndexOf(handle);
					if (find != apis.Count - 1)
						apis[find] = apis[^1];
					apis.RemoveAt(apis.Count - 1);
				}
			}
		}

		internal static void Initiate<TApi>(int index, Type actualType) where TApi : IAbstractRuntimeApi<TApi>
		{
			lock (__locker)
			{
				var instance = IAbstractRuntimeApi<TApi>.Create(actualType);
				var old = AbstractApiSelector<TApi>.APIs[index];
				AbstractApiSelector<TApi>.APIs[index] = instance;
				old.Dispose();
				var list = InstanceApis[old];
				InstanceApis.Remove(old);
				InstanceApis[instance] = list;
			}
		}
	}
	#endregion

	/// <summary>
	/// The base abstract class for all API selector classes defined in and out of this assembly.
	/// </summary>
	/// <typeparam name="TApi">Any runtime API abstract class which inherits <see cref="IAbstractRuntimeApi{TApi}"/></typeparam>
	public abstract class AbstractApiSelector<TApi> where TApi : IAbstractRuntimeApi<TApi>
	{
		#region basic
		/// <summary>
		/// Get the currently using <typeparamref name="TApi"/>.
		/// </summary>
		/// <remarks>DO NOT directly access the method of it unless you are sure what you are doing.</remarks>
		public static TApi? Current => CurrentApiIndex >= 0 ? ApiManager.Get<TApi>(CurrentApiIndex) : default;

		internal static readonly List<TApi> APIs = new(); 

		private static int CurrentApiIndex = -1;

		private static readonly object apiChangeLock = new();
		#endregion

		#region enumerate
		/// <summary>
		/// An empty class only used as an API enumerable
		/// </summary>
		protected sealed class ApiEnumerableClass
		{
			/// <summary>
			/// Get a <see cref="ApiEnumerator"/> that enumerates through all API instances of <typeparamref name="TApi"/> kind
			/// </summary>
			/// <returns>A new <see cref="ApiEnumerator"/> that enumerates through all API instances of <typeparamref name="TApi"/> kind</returns>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public ApiEnumerator GetEnumerator() => new();
		}

		/// <summary>
		/// Get the API enumerable of the APIs
		/// </summary>
		protected static readonly ApiEnumerableClass ApiEnumerable = new();

		/// <summary>
		/// The enumerator used to enumerates through all API instances of <typeparamref name="TApi"/> kind
		/// </summary>
		/// <remarks>The access for APIs is locked when enumerating this <see cref="ApiEnumerator"/></remarks>
		protected ref struct ApiEnumerator
		{
			private int current = 0;

			private static int EndIndex
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => (CurrentApiIndex + 1) % ApiManager.Count<TApi>();
			}

			/// <summary>
			/// Get the current API instance
			/// </summary>
			public TApi Current
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => ApiManager.Get<TApi>(current);
			}

			/// <summary>
			/// The default constructor of <see cref="ApiEnumerator"/>
			/// </summary>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public ApiEnumerator()
			{
				this.Reset();
			}

			/// <summary>
			/// Sets the API enumerator to its initial position, which is before the first API in the API collection.
			/// </summary>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Reset()
			{
				this.current = CurrentApiIndex - 1;
				if (this.current < 0)
					this.current = ApiManager.Count<TApi>() - 1;
			}

			/// <summary>
			/// Advances the API enumerator to the next API instance of the API collection.
			/// </summary>
			/// <returns>true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool MoveNext()
			{
				do
				{
					this.current++;
					if (this.current == ApiManager.Count<TApi>())
						current = 0;
					if (this.current == EndIndex)
						return false;
				} while (this.Current.Disposed);
				return true;
			}
		}

		/// <summary>
		/// Set the current API implementation among all to a given <paramref name="implementation"/>
		/// </summary>
		/// <param name="implementation">The implementation which implements <typeparamref name="TApi"/></param>
		/// <exception cref="ArgumentException">If <paramref name="implementation"/> does not implements <typeparamref name="TApi"/> with empty constructor</exception>
		/// <exception cref="ObjectDisposedException">If <paramref name="implementation"/> is disposed</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void SetImplementation(TApi implementation)
		{
			if (implementation is null || implementation.Disposed)
				throw new ObjectDisposedException(nameof(implementation));
			lock (apiChangeLock)
			{
				int newIndex = ApiManager.IndexOf(implementation);
				if (newIndex < 0)
				{
					ApiManager.Add(implementation);
					newIndex = ApiManager.Count<TApi>() - 1;
				}
				if (Settings.DisposeNotCurrentImplementation)
				{
					ApiManager.Dispose<TApi>(CurrentApiIndex);
				}
				CurrentApiIndex = newIndex;
				if (ApiManager.Get<TApi>(CurrentApiIndex).Disposed)
				{
					ApiManager.Initiate<TApi>(CurrentApiIndex, implementation.GetType());
				}
			}
		}
		#endregion
	}
}
