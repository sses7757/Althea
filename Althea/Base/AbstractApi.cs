using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Althea
{
	/// <summary>
	/// The base abstract interface for all runtime API classes defined in and out of this assembly. Any concrete runtime API class shall inherit the sub-classes of this class.
	/// </summary>
	/// <typeparam name="TApi">Any runtime API abstract class which inherits <see cref="IAbstractRuntimeApi{TApi}"/></typeparam>
	/// <remarks>Typically, the <b>callers</b> are responsible for checking the input parameters of all the defined API methods.</remarks>
	public interface IAbstractRuntimeApi<TApi> : IDisposable where TApi : IAbstractRuntimeApi<TApi>
	{
		#region basic
		/// <summary>
		/// When implemented by a derived class, get a <see cref="bool"/> indicating whether this instance is disposed or not
		/// </summary>
		public bool Disposed { get; }

		private delegate TApi CreateDelegate();

		private static readonly Dictionary<Type, CreateDelegate> CreateDelegates = new();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static TApi Create<TImpl>() where TImpl : TApi, new() => new TImpl();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static TApi Create(Type type)
		{
			if (!CreateDelegates.ContainsKey(type))
			{
				var method = typeof(IAbstractRuntimeApi<TApi>).GetMethod(nameof(Create), 1, BindingFlags.NonPublic | BindingFlags.Static, null, System.Array.Empty<Type>(), null)?.MakeGenericMethod(type);
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
			if (index < 0)
				return;
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
		public static TApi? Current => CurrentApiIndex >= 0 ? APIs[CurrentApiIndex] : default;

		internal static readonly List<TApi> APIs = new(); 

		private static int CurrentApiIndex = -1;

		private static readonly ReaderWriterLockSlim apiLock = new();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void SetImplementation(TApi implementation!!)
		{
			if (implementation.Disposed)
				throw new ObjectDisposedException(nameof(implementation));
			try
			{
				apiLock.EnterWriteLock();
				int newIndex = APIs.IndexOf(implementation);
				if (newIndex < 0)
				{
					ApiManager.Add(implementation);
					newIndex = APIs.Count - 1;
				}
				if (Settings.DisposeNotCurrentImplementation)
				{
					ApiManager.Dispose<TApi>(CurrentApiIndex);
				}
				CurrentApiIndex = newIndex;
				if (APIs[CurrentApiIndex].Disposed)
				{
					ApiManager.Initiate<TApi>(CurrentApiIndex, implementation.GetType());
				}
			}
			finally
			{
				apiLock.ExitWriteLock();
			}
		}
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

			/// <summary>
			/// Get all current <typeparamref name="TApi"/>s.
			/// </summary>
			public IEnumerable<TApi> CurrentApis
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get
				{
					List<TApi> apis = new(APIs.Count);
					apiLock.EnterReadLock();
					try
					{
						var enumerator = GetEnumerator();
						do
						{
							apis.Add(enumerator.Current);
						} while (enumerator.MoveNext());
						return apis;
					}
					finally
					{
						apiLock.ExitReadLock();
					}
				}
			}
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
			private bool disposed = false;

			private static int EndIndex
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => (CurrentApiIndex + 1) % APIs.Count;
			}

			/// <summary>
			/// Get the current API instance
			/// </summary>
			public TApi Current
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => this.disposed ? throw new ObjectDisposedException(nameof(ApiEnumerator)) : APIs[current];
			}

			/// <summary>
			/// The default constructor of <see cref="ApiEnumerator"/> that starts the enumeration and enters API reader lock.
			/// </summary>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public ApiEnumerator()
			{
				apiLock.EnterReadLock();
				this.Reset();
			}

			/// <summary>
			/// The disposer of <see cref="ApiEnumerator"/> that simply exits the API reader lock.
			/// </summary>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Dispose()
			{
				if (this.disposed)
					throw new ObjectDisposedException(nameof(ApiEnumerator));
				apiLock.ExitReadLock();
				this.disposed = true;
			}

			/// <summary>
			/// Sets the API enumerator to its initial position, which is before the first API in the API collection.
			/// </summary>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Reset()
			{
				if (this.disposed)
					throw new ObjectDisposedException(nameof(ApiEnumerator));
				this.current = CurrentApiIndex - 1;
				if (this.current < 0)
					this.current = APIs.Count - 1;
			}

			/// <summary>
			/// Advances the API enumerator to the next API instance of the API collection.
			/// </summary>
			/// <returns>true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool MoveNext()
			{
				if (this.disposed)
					throw new ObjectDisposedException(nameof(ApiEnumerator));
				do
				{
					this.current++;
					if (this.current == APIs.Count)
						current = 0;
					if (this.current == EndIndex)
						return false;
				} while (this.Current.Disposed);
				return true;
			}
		}
		#endregion
	}
}
