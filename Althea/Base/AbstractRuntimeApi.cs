using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.Linq;
using Althea.Helpers;


namespace Althea
{
	/// <summary>
	/// The base abstract class for all runtime API classes defined in and out of this assembly
	/// </summary>
	/// <remarks>
	/// Typically, the <b>caller</b> are responsible for checking the input parameters of these methods.<br/>
	/// Typically, the inherited concrete classes shall perform lazy initializations if possible, because the instances created by default constructors may be held by the abstract API classes for a long time such that they can find the suitable candidates easily.
	/// </remarks>
	public abstract class AbstractRuntimeApi : IDisposable
	{
		#region static methods used for creating API class instances
		private static T Create<T>(Type type) where T : AbstractRuntimeApi
		{
			if (type.IsGenericType || type.IsAbstract || !type.IsAssignableTo(typeof(T)))
			{
				throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(type));
			}
			var constructor = type.GetConstructor(Array.Empty<Type>());
			if (constructor is not null)
			{
				if (constructor.Invoke(Array.Empty<object>()) is not T result)
					throw new InvalidOperationException(Resources.Backend.CannotInitialize);
				return result;
			}
			else
			{
				var constructors = type.GetConstructors();
				if (constructors.Length == 0 || !constructors.Contains(0, c => c.GetParameters().Length))
					throw new InvalidOperationException(Resources.Backend.CannotInitialize);
				if (constructors.Where(c => c.GetParameters().Length == 0)[0].Invoke(Array.Empty<object>()) is not T result)
					throw new InvalidOperationException(Resources.Backend.CannotInitialize);
				return result;
			}
		}

		private static void Initialize<T>(LinkedListNode<T> node) where T : AbstractRuntimeApi
		{
			if (node is null)
				throw new ArgumentNullException(nameof(node));
			if (!node.Value.Disposed)
			{
				return;
			}
			var type = node.Value.GetType();
			node.Value = Create<T>(type);
		}

		private static void PromoteImplementation<T>(LinkedList<T> recents, T impl) where T : AbstractRuntimeApi
		{
			if (recents is null || impl is null)
				return;
			if (!recents.Contains(impl))
				return;
			recents.Remove(impl);
			var node = recents.AddFirst(impl);
			Initialize(node);
			if (Settings.DisposeNotCurrentImplementation)
			{
				node.Next?.Value?.Dispose();
			}
		}

		/// <summary>
		/// Set the current implementation in <paramref name="recents"/> (the first node) to a given <paramref name="implementation"/>
		/// </summary>
		/// <typeparam name="T">any API abstract class which implements <see cref="AbstractRuntimeApi"/></typeparam>
		/// <param name="recents">The <see cref="LinkedList{T}"/> of recent APIs to operate on</param>
		/// <param name="implementation">The implementation indicated by a <see cref="Type"/></param>
		/// <returns>Success or not</returns>
		protected static bool SetImplementation<T>(LinkedList<T> recents, Type implementation) where T : AbstractRuntimeApi
		{
			if (implementation.IsGenericType || implementation.IsAbstract || !implementation.IsAssignableTo(typeof(T)))
			{
				return false;
			}
			// otherwise
			var current = recents.First;
			while (current is not null)
			{
				if (current.Value.GetType() == implementation)
				{
					PromoteImplementation(recents, current.Value);
					return true;
				}
				current = current.Next;
			}
			// a new implementation
			var node = recents.AddFirst(Create<T>(implementation));
			if (Settings.DisposeNotCurrentImplementation)
			{
				node.Next?.Value?.Dispose();
			}
			return true;
		}

		/// <summary>
		/// Set the current implementation in <paramref name="recents"/> (the first node) to a given <paramref name="implementation"/>
		/// </summary>
		/// <typeparam name="T">any API abstract class which implements <see cref="AbstractRuntimeApi"/></typeparam>
		/// <param name="recents">The <see cref="LinkedList{T}"/> of recent APIs to operate on</param>
		/// <param name="implementation">The implementation indicated by a <typeparamref name="T"/></param>
		protected static void SetImplementation<T>(LinkedList<T> recents, T implementation) where T : AbstractRuntimeApi
		{
			if (recents.Contains(implementation))
			{
				PromoteImplementation(recents, implementation);
			}
			else
			{
				var node = recents.AddFirst(implementation);
				if (Settings.DisposeNotCurrentImplementation)
				{
					node.Next?.Value?.Dispose();
				}
			}
		}
		#endregion

		#region static methods used for dispatching
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void DisposeNotCurrent<T>(LinkedList<T> recents) where T : AbstractRuntimeApi
		{
			if (Settings.DisposeNotCurrentImplementation)
			{
				var node = recents.First;
				while (node is not null)
				{
					node.Value.Dispose();
					node = node.Next;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void Check<T>(LinkedList<T> recents) where T : AbstractRuntimeApi
		{
			if (recents is null)
				throw new ArgumentNullException(nameof(recents));
			if (recents.Count == 0)
				throw new InvalidOperationException(Resources.Backend.NotAvailable);
			DisposeNotCurrent(recents);
		}

		/// <summary>
		/// Check the given recent API list <paramref name="recents"/> and the validness of given storage(s)
		/// </summary>
		/// <typeparam name="T">The API class type</typeparam>
		/// <param name="recents">The recent API list as a <see cref="LinkedList{T}"/></param>
		/// <param name="storage1">The first storage to check validness</param>
		/// <exception cref="ArgumentNullException">If any of the given storage(s) is null or invalid</exception>
		/// <exception cref="InvalidOperationException">If <paramref name="recents"/> is empty (there is not available back-end)</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void Check<T>(LinkedList<T> recents, IStorage storage1) where T : AbstractRuntimeApi
		{
			if (storage1 is null || storage1.IsValid())
				throw new ArgumentNullException(nameof(storage1));
			Check(recents);
		}

		/// <summary>
		/// Check the given recent API list <paramref name="recents"/> and the validness of given storage(s)
		/// </summary>
		/// <typeparam name="T">The API class type</typeparam>
		/// <param name="recents">The recent API list as a <see cref="LinkedList{T}"/></param>
		/// <param name="storage1">The first storage to check validness</param>
		/// <param name="storage2">The second storage to check validness</param>
		/// <exception cref="ArgumentNullException">If any of the given storage(s) is null or invalid</exception>
		/// <exception cref="InvalidOperationException">If <paramref name="recents"/> is empty (there is not available back-end)</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void Check<T>(LinkedList<T> recents, IStorage storage1, IStorage storage2) where T : AbstractRuntimeApi
		{
			if (storage1 is null || storage1.IsValid())
				throw new ArgumentNullException(nameof(storage1));
			if (storage2 is null || storage2.IsValid())
				throw new ArgumentNullException(nameof(storage2));
			Check(recents);
		}

		/// <summary>
		/// Check the given recent API list <paramref name="recents"/> and the validness of given storage(s)
		/// </summary>
		/// <typeparam name="T">The API class type</typeparam>
		/// <param name="recents">The recent API list as a <see cref="LinkedList{T}"/></param>
		/// <param name="storage1">The first storage to check validness</param>
		/// <param name="storage2">The second storage to check validness</param>
		/// <param name="storage3">The third storage to check validness</param>
		/// <exception cref="ArgumentNullException">If any of the given storage(s) is null or invalid</exception>
		/// <exception cref="InvalidOperationException">If <paramref name="recents"/> is empty (there is not available back-end)</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void Check<T>(LinkedList<T> recents, IStorage storage1, IStorage storage2, IStorage storage3) where T : AbstractRuntimeApi
		{
			if (storage1 is null || storage1.IsValid())
				throw new ArgumentNullException(nameof(storage1));
			if (storage2 is null || storage2.IsValid())
				throw new ArgumentNullException(nameof(storage2));
			if (storage3 is null || storage3.IsValid())
				throw new ArgumentNullException(nameof(storage3));
			Check(recents);
		}

		/// <summary>
		/// Check the given recent API list <paramref name="recents"/> and the validness of given storage(s)
		/// </summary>
		/// <typeparam name="T">The API class type</typeparam>
		/// <param name="recents">The recent API list as a <see cref="LinkedList{T}"/></param>
		/// <param name="storage1">The first storage to check validness</param>
		/// <param name="storage2">The second storage to check validness</param>
		/// <param name="storage3">The third storage to check validness</param>
		/// <param name="storage4">The fourth storage to check validness</param>
		/// <exception cref="ArgumentNullException">If any of the given storage(s) is null or invalid</exception>
		/// <exception cref="InvalidOperationException">If <paramref name="recents"/> is empty (there is not available back-end)</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void Check<T>(LinkedList<T> recents, IStorage storage1, IStorage storage2, IStorage storage3, IStorage storage4) where T : AbstractRuntimeApi
		{
			if (storage1 is null || storage1.IsValid())
				throw new ArgumentNullException(nameof(storage1));
			if (storage2 is null || storage2.IsValid())
				throw new ArgumentNullException(nameof(storage2));
			if (storage3 is null || storage3.IsValid())
				throw new ArgumentNullException(nameof(storage3));
			if (storage4 is null || storage4.IsValid())
				throw new ArgumentNullException(nameof(storage4));
			Check(recents);
		}

		/// <summary>
		/// Check the given recent API list <paramref name="recents"/> and the validness of given storages
		/// </summary>
		/// <typeparam name="T">The API class type</typeparam>
		/// <param name="recents">The recent API list as a <see cref="LinkedList{T}"/></param>
		/// <param name="storages">The storages to check validness</param>
		/// <exception cref="ArgumentNullException">If any of the given <paramref name="storages"/> is null or invalid</exception>
		/// <exception cref="InvalidOperationException">If <paramref name="recents"/> is empty (there is not available back-end)</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void Check<T>(LinkedList<T> recents, IReadOnlyList<IStorage> storages) where T : AbstractRuntimeApi
		{
			if (storages is null || storages.Any(static s => s is null || !s.IsValid()))
				throw new ArgumentNullException(nameof(storages));
			Check(recents);
		}

		/// <summary>
		/// Select the most recent implementation in <paramref name="recents"/> which fits the given predicate <paramref name="validApi"/>
		/// </summary>
		/// <typeparam name="T">any API abstract class which implements <see cref="AbstractRuntimeApi"/></typeparam>
		/// <param name="recents">The <see cref="LinkedList{T}"/> of recent APIs to select in.</param>
		/// <param name="validApi">A <see cref="Predicate{T}"/> used to check other validness of the candidate implementations</param>
		/// <param name="from">The starting <see cref="LinkedListNode{T}"/> to begin searching, default null means search from the first of <paramref name="recents"/></param>
		/// <returns>The suitable most recent implementation as a <see cref="LinkedListNode{T}"/> or error if not found.</returns>
		/// <exception cref="InvalidOperationException">if there is no available back-end implementation or the suitable one cannot be initialized</exception>
		protected static LinkedListNode<T> SelectImplementation<T>(LinkedList<T> recents, Predicate<T> validApi, LinkedListNode<T>? from = null) where T : AbstractRuntimeApi
		{
			if (validApi is null)
				throw new ArgumentNullException(nameof(validApi));
			Check(recents);

			var current = from?.Next ?? recents.First;
			if (current is null)
				throw new InvalidOperationException(Resources.Backend.NotAvailable);
			if (validApi.Invoke(current.Value))
			{
				return current;
			}
			current = current.Next;
			while (current is not null)
			{
				Initialize(current);
				if (validApi.Invoke(current.Value))
				{
					return current;
				}
				current = current.Next;
			}
			throw new InvalidOperationException(Resources.Backend.NotAvailable);
		}
		#endregion

		#region static methods used for generating support information
		/// <summary>
		/// Generate a list of unary <see cref="CombinationOfLocations"/>s from given "pure" <see cref="StorageLocation"/>s by enumerating all possible combinations of all possible lengths
		/// </summary>
		/// <param name="locations">The given "pure" <see cref="StorageLocation"/>s</param>
		/// <returns>The generated list of <see cref="CombinationOfLocations"/>s, or null if <paramref name="locations"/> is null or empty</returns>
		/// <exception cref="OverflowException">If the length of <paramref name="locations"/> is larger than 64</exception>
		/// <remarks>Although the functionality of this method can be done by <see cref="GenerateNaryLoactions"/>, this one is specially separated for performance issues.</remarks>
		protected static CombinationOfLocations[] GenerateUnaryLoactions(Span<StorageLocation> locations)
		{
			if (locations.IsEmpty)
				return Array.Empty<CombinationOfLocations>();

			byte length = checked((byte)locations.Length);
			long max = checked(1 << length);
			CombinationOfLocations[] combinations = new CombinationOfLocations[max - 1];
			Span<StorageLocation> combination = stackalloc StorageLocation[length];
			for (long i = 0; i < max; i++)
			{
				int k = 0;
				for (byte j = 0; j < length; j++)
				{
					if (i.IsBitSet(j))
					{
						combination[k++] = locations[j];
					}
				}
				combinations[i - 1] = new CombinationOfLocations(CombinationType.PureOrMixed, combination[..k]);
			}
			return combinations;
		}

		/// <summary>
		/// Generate a list of binary (<see cref="CombinationOfLocations"/>1, <see cref="CombinationOfLocations"/>2)s from given "pure" <see cref="StorageLocation"/>s by enumerating all possible combinations of all possible lengths
		/// </summary>
		/// <param name="locations">The given "pure" <see cref="StorageLocation"/>s</param>
		/// <returns>The generated list of <see cref="CombinationOfLocations"/>'s binaries, or null if <paramref name="locations"/> is null or has less than two elements</returns>
		/// <exception cref="OverflowException">If the length of <paramref name="locations"/> is larger than 32</exception>
		/// <remarks>Although the functionality of this method can be done by <see cref="GenerateNaryLoactions"/>, this one is specially separated for performance issues.</remarks>
		protected static ImmutableTwoElementSet<CombinationOfLocations>[] GenerateBinaryLoactions(Span<StorageLocation> locations)
		{
			if (locations.Length < 2)
				return Array.Empty<ImmutableTwoElementSet<CombinationOfLocations>>();

			int unaryLength = locations.Length;
			long max = ExtensionHelper.CombinationNumber(2, 1 + unaryLength); // binomial of (unaryLength + 1, 2)
			var combinations = new ImmutableTwoElementSet<CombinationOfLocations>[max];
			var unary = GenerateUnaryLoactions(locations);
			long n = 0;
			for (long i = 0; i < unaryLength; i++)
			{
				for (long j = 0; j <= i; j++)
				{
					combinations[n++] = new ImmutableTwoElementSet<CombinationOfLocations>(unary[i], unary[j]);
				}
			}
			return combinations;
		}

		/// <summary>
		/// Generate a list of ternary (<see cref="CombinationOfLocations"/>1, <see cref="CombinationOfLocations"/>2, <see cref="CombinationOfLocations"/>3)s from given "pure" <see cref="StorageLocation"/>s by enumerating all possible combinations of all possible lengths
		/// </summary>
		/// <param name="locations">The given "pure" <see cref="StorageLocation"/>s</param>
		/// <returns>The generated list of <see cref="CombinationOfLocations"/>'s ternaries, or null if <paramref name="locations"/> is null or has less than three elements</returns>
		/// <exception cref="OverflowException">If the length of <paramref name="locations"/> is larger than 21</exception>
		/// <remarks>Although the functionality of this method can be done by <see cref="GenerateNaryLoactions"/>, this one is specially separated for performance issues.</remarks>
		protected static ImmutableThreeElementSet<CombinationOfLocations>[] GenerateTernaryLoactions(Span<StorageLocation> locations)
		{
			if (locations.Length < 3)
				return Array.Empty<ImmutableThreeElementSet<CombinationOfLocations>>();

			int unaryLength = locations.Length;
			long max = ExtensionHelper.CombinationNumber(3, 2 + unaryLength); // binomial (unaryLength + 2, 3)
			var combinations = new ImmutableThreeElementSet<CombinationOfLocations>[max];
			var unary = GenerateUnaryLoactions(locations);
			long n = 0;
			for (int i = 0; i < unary.Length; i++)
			{
				for (int j = 0; j <= i; j++)
				{
					for (int k = 0; k <= j; i++)
					{
						combinations[n++] = new ImmutableThreeElementSet<CombinationOfLocations>(unary[i], unary[j], unary[k]);
					}
				}
			}
			return combinations;
		}

		// Ignore Spelling: N-ary N-arys
		/// <summary>
		/// Generate a list of N-ary (<see cref="CombinationOfLocations"/>_1, ..., <see cref="CombinationOfLocations"/>_<paramref name="N"/>)s from given "pure" <see cref="StorageLocation"/>s by enumerating all possible combinations of all possible lengths
		/// </summary>
		/// <param name="N">The number of operands, must be positive</param>
		/// <param name="locations">The given "pure" <see cref="StorageLocation"/>s</param>
		/// <returns>The generated list of <see cref="CombinationOfLocations"/>'s N-arys; null if <paramref name="locations"/> is null, or <paramref name="locations"/> has less than <paramref name="N"/> elements</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="N"/> is not a positive number</exception>
		/// <exception cref="OverflowException">If the length of <paramref name="locations"/> is larger than 64 / <paramref name="N"/></exception>
		protected static IImmutableSet<CombinationOfLocations>[] GenerateNaryLoactions(int N, Span<StorageLocation> locations)
		{
			if (N <= 0)
				throw new ArgumentOutOfRangeException(nameof(N), Resources.Parameter.MustPositive);
			if (locations.Length < N)
				return Array.Empty<IImmutableSet<CombinationOfLocations>>();

			int unaryLength = locations.Length;
			long max = ExtensionHelper.CombinationNumber(N, N + unaryLength - 1); // binomial (unaryLength + N - 1, N)
			var combinations = new IImmutableSet<CombinationOfLocations>[max];
			var unary = GenerateUnaryLoactions(locations);
			Span<int> indices = stackalloc int[N];
			for (long n = 0; n < max; n++)
			{
				CombinationOfLocations[] set = new CombinationOfLocations[N];
				// create set
				for (int i = 0; i < N; i++)
				{
					set[i] = unary[indices[i]];
				}
				// set value
				combinations[n] = new ImmutableSet<CombinationOfLocations>(set);
				// increase indices
				for (int i = N - 1; i > 0; i--)
				{
					if (indices[i] > indices[i - 1])
					{
						indices[i] = 0;
						indices[i - 1]++;
					}
				}
			}
			return combinations;
		}
		#endregion


		#region basic
		/// <summary>
		/// Whether this class is disposed or not
		/// </summary>
		protected bool Disposed { get; private set; } = false;

		/// <summary>
		/// Release all unmanaged resources held by this class
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
			this.Disposed = true;
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// When implemented, the method used to actually dispose 
		/// </summary>
		/// <param name="disposeManaged"></param>
		protected abstract void Dispose(bool disposeManaged);
		#endregion
	}
}
