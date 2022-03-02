using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

using Althea.Helpers;
using Althea.Linq;


namespace Althea
{
	/// <summary>
	/// The base abstract class for all runtime API classes defined in and out of this assembly. Any concrete runtime API class shall inherit the sub-classes of this class.<br/>
	/// The derived concrete class(es) shall be able to serialized and deserialized by <see cref="JsonSerializer"/> (typically through indicating <see cref="JsonConstructorAttribute"/> and implementing <see cref="CurrentConverter"/>).
	/// </summary>
	/// <typeparam name="TApi">Any runtime API abstract class which inherits <see cref="AbstractRuntimeApi{TApi}"/></typeparam>
	/// <remarks>Typically, the <b>callers</b> are responsible for checking the input parameters of all the defined API methods.</remarks>
	public abstract class AbstractRuntimeApi<TApi> : IDisposable where TApi : AbstractRuntimeApi<TApi>
	{
		/// <summary>
		/// Get a <see cref="bool"/> indicating whether this instance is disposed or not
		/// </summary>
		public bool Disposed { get; protected set; } = false;

		/// <summary>
		/// Release all unmanaged resources held by this class. When overridden by a derived class, the disposition behavior can be modified.
		/// </summary>
		public virtual void Dispose()
		{
			this.Dispose(true);
			this.Disposed = true;
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// When implemented by a derived class, actually the release all unmanaged resources held by this runtime API instance.
		/// </summary>
		/// <param name="invokedByUser">Whether this method is invoked by user or by GC</param>
		protected abstract void Dispose(bool invokedByUser);

		/// <summary>
		/// When implemented by a derived class, get the <see cref="JsonConverter{T}"/> used to serialize it (deserialization is done via default of <see cref="JsonSerializer"/> or <see cref="JsonConstructorAttribute"/>). The default implementation simply returns null.
		/// </summary>
		protected internal virtual JsonConverter<AbstractRuntimeApi<TApi>>? CurrentConverter => null;

		private delegate TApi CreateDelegate();

		private static readonly Dictionary<Type, CreateDelegate> CreateDelegates = new();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static TApi Create<TImpl>() where TImpl : TApi, new() => new TImpl();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static TApi Create(Type type)
		{
			if (!CreateDelegates.ContainsKey(type))
			{
				var method = typeof(AbstractRuntimeApi<TApi>)
				.GetMethod(nameof(Create), 1,
					BindingFlags.NonPublic | BindingFlags.Static,
					null, Array.Empty<Type>(), null)?
				.MakeGenericMethod(type);
				if (method is null)
					throw new ArgumentException(Resources.Parameter.UnexpectedType, nameof(type));
				CreateDelegates[type] = method.CreateDelegate<CreateDelegate>();
			}
			return CreateDelegates[type].Invoke();
		}
	}

	/// <summary>
	/// The base abstract class for all API selector classes defined in and out of this assembly.
	/// </summary>
	/// <typeparam name="TApi">Any runtime API abstract class which inherits <see cref="AbstractRuntimeApi{TApi}"/></typeparam>
	public abstract class AbstractApiSelector<TApi> where TApi : AbstractRuntimeApi<TApi>
	{
		#region basic
		/// <summary>
		/// The currently using <typeparamref name="TApi"/>
		/// </summary>
		public static TApi? Current => CurrentApiIndex >= 0 ? APIs[CurrentApiIndex] : null;

		/// <summary>
		/// The recently used APIs (of type <typeparamref name="TApi"/>)
		/// </summary>
		protected static readonly List<TApi> APIs = new();

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
			public ApiEnumerator GetEnumerator() => new();
		}

		/// <summary>
		/// Get the API enumerable of the APIs
		/// </summary>
		protected static readonly ApiEnumerableClass ApiEnumerable = new();

		/// <summary>
		/// The enumerator used to enumerates through all API instances of <typeparamref name="TApi"/> kind
		/// </summary>
		/// <remarks>The access for <see cref="APIs"/> is locked when enumerating this <see cref="ApiEnumerator"/></remarks>
		protected struct ApiEnumerator : IEnumerator<TApi>, IDisposable
		{
			private int current = 0;

			private readonly bool __locked = false;

			private static int EndIndex => (CurrentApiIndex + 1) % APIs.Count;

			/// <summary>
			/// Get the current API instance
			/// </summary>
			public TApi Current => APIs[current];

			object IEnumerator.Current => this.Current;

			/// <summary>
			/// The default constructor of <see cref="ApiEnumerator"/>
			/// </summary>
			public ApiEnumerator()
			{
				this.Reset();
				Monitor.Enter(apiChangeLock, ref this.__locked);
			}

			/// <summary>
			/// Sets the API enumerator to its initial position, which is before the first API in the API collection.
			/// </summary>
			public void Reset()
			{
				this.current = CurrentApiIndex - 1;
				if (this.current < 0)
					this.current = APIs.Count - 1;
			}

			/// <summary>
			/// Dispose the enumerator instance
			/// </summary>
			public void Dispose()
			{
				if (this.__locked)
					Monitor.Exit(apiChangeLock);
			}

			/// <summary>
			/// Advances the API enumerator to the next API instance of the API collection.
			/// </summary>
			/// <returns>true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
			public bool MoveNext()
			{
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

		/// <summary>
		/// Set the current API implementation among all to a given <paramref name="implementation"/>
		/// </summary>
		/// <param name="implementation">The implementation which implements <typeparamref name="TApi"/></param>
		/// <exception cref="ArgumentException">If <paramref name="implementation"/> does not implements <typeparamref name="TApi"/> with empty constructor</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void SetImplementation(TApi implementation)
		{
			lock (apiChangeLock)
			{
				int newIndex = APIs.IndexOf(implementation);
				if (newIndex < 0)
				{
					APIs.Add(implementation);
					newIndex = APIs.Count - 1;
				}
				if (Settings.DisposeNotCurrentImplementation)
				{
					APIs[CurrentApiIndex].Dispose();
				}
				CurrentApiIndex = newIndex;
				if (APIs[CurrentApiIndex].Disposed)
				{
					APIs[CurrentApiIndex] = AbstractRuntimeApi<TApi>.Create(implementation.GetType());
				}
			}
		}
		#endregion

		#region static methods used for checking
		/// <summary>
		/// Check the given recent API list <see cref="APIs"/> and the validness of given storage(s)
		/// </summary>
		/// <param name="storage1">The first storage to check validness</param>
		/// <exception cref="ArgumentNullException">If any of the given storage(s) is null or invalid</exception>
		/// <exception cref="InvalidOperationException">If <see cref="APIs"/> is empty (there is not available back-end)</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void Check(IStorage storage1)
		{
			if (storage1 is null || !storage1.IsValid())
				throw new ArgumentNullException(nameof(storage1));
		}

		/// <summary>
		/// Check the given recent API list <see cref="APIs"/> and the validness of given storage(s)
		/// </summary>
		/// <param name="storage1">The first storage to check validness</param>
		/// <param name="storage2">The second storage to check validness</param>
		/// <exception cref="ArgumentNullException">If any of the given storage(s) is null or invalid</exception>
		/// <exception cref="InvalidOperationException">If <see cref="APIs"/> is empty (there is not available back-end)</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void Check(IStorage storage1, IStorage storage2)
		{
			if (storage1 is null || !storage1.IsValid())
				throw new ArgumentNullException(nameof(storage1));
			if (storage2 is null || !storage2.IsValid())
				throw new ArgumentNullException(nameof(storage2));
		}

		/// <summary>
		/// Check the given recent API list <see cref="APIs"/> and the validness of given storage(s)
		/// </summary>
		/// <param name="storage1">The first storage to check validness</param>
		/// <param name="storage2">The second storage to check validness</param>
		/// <param name="storage3">The third storage to check validness</param>
		/// <exception cref="ArgumentNullException">If any of the given storage(s) is null or invalid</exception>
		/// <exception cref="InvalidOperationException">If <see cref="APIs"/> is empty (there is not available back-end)</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void Check(IStorage storage1, IStorage storage2, IStorage storage3)
		{
			if (storage1 is null || !storage1.IsValid())
				throw new ArgumentNullException(nameof(storage1));
			if (storage2 is null || !storage2.IsValid())
				throw new ArgumentNullException(nameof(storage2));
			if (storage3 is null || !storage3.IsValid())
				throw new ArgumentNullException(nameof(storage3));
		}

		/// <summary>
		/// Check the given recent API list <see cref="APIs"/> and the validness of given storage(s)
		/// </summary>
		/// <param name="storage1">The first storage to check validness</param>
		/// <param name="storage2">The second storage to check validness</param>
		/// <param name="storage3">The third storage to check validness</param>
		/// <param name="storage4">The fourth storage to check validness</param>
		/// <exception cref="ArgumentNullException">If any of the given storage(s) is null or invalid</exception>
		/// <exception cref="InvalidOperationException">If <see cref="APIs"/> is empty (there is not available back-end)</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void Check(IStorage storage1, IStorage storage2, IStorage storage3, IStorage storage4)
		{
			if (storage1 is null || !storage1.IsValid())
				throw new ArgumentNullException(nameof(storage1));
			if (storage2 is null || !storage2.IsValid())
				throw new ArgumentNullException(nameof(storage2));
			if (storage3 is null || !storage3.IsValid())
				throw new ArgumentNullException(nameof(storage3));
			if (storage4 is null || !storage4.IsValid())
				throw new ArgumentNullException(nameof(storage4));
		}

		/// <summary>
		/// Check the given recent API list <see cref="APIs"/> and the validness of given storages
		/// </summary>
		/// <param name="storages">The storages to check validness</param>
		/// <exception cref="ArgumentNullException">If any of the given <paramref name="storages"/> is null or invalid</exception>
		/// <exception cref="InvalidOperationException">If <see cref="APIs"/> is empty (there is not available back-end)</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void Check(params IStorage[] storages)
		{
			if (storages is null || !storages.Any(static s => s is null || !s.IsValid()))
				throw new ArgumentNullException(nameof(storages));
		}
		#endregion
	}
}
