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
		/// <summary>
		/// Create an instance of <typeparamref name="T"/> which has a constructor with no parameters.
		/// </summary>
		/// <typeparam name="T">any API abstract class which implements <see cref="AbstractRuntimeApi"/></typeparam>
		/// <param name="type">The <see cref="Type"/> (not the type of used to initialize</param>
		/// <returns>The created instance of <typeparamref name="T"/>, or null if <typeparamref name="T"/> does not have a constructor with no parameters.</returns>
		protected static T Create<T>(Type type) where T : AbstractRuntimeApi
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

		/// <summary>
		/// Initialize a <see cref="LinkedListNode{T}"/> of <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">any API abstract class which implements <see cref="AbstractRuntimeApi"/></typeparam>
		/// <param name="node">The <see cref="LinkedListNode{T}"/> to initialize</param>
		protected static void Initialize<T>(LinkedListNode<T> node) where T : AbstractRuntimeApi
		{
			if (node.Value is null)
				throw new ArgumentNullException(nameof(node));
			if (!node.Value.Disposed)
			{
				return;
			}
			var type = node.Value.GetType();
			node.Value = Create<T>(type);
		}

		/// <summary>
		/// Promote the implementation in <paramref name="node"/> to the top of the linked list <paramref name="recents"/>
		/// </summary>
		/// <typeparam name="T">any API abstract class which implements <see cref="AbstractRuntimeApi"/></typeparam>
		/// <param name="recents">The <see cref="LinkedList{T}"/> of recent APIs to operate on</param>
		/// <param name="node">The <see cref="LinkedListNode{T}"/> to promote</param>
		protected static void PromoteImplementation<T>(LinkedList<T> recents, LinkedListNode<T> node) where T : AbstractRuntimeApi
		{
			if (recents is null || node is null)
				return;
			if (!recents.Contains(node.Value))
				return;
			recents.Remove(node.Value);
			node = recents.AddFirst(node.Value);
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
					PromoteImplementation(recents, current);
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
		/// <returns>Success or not</returns>
		protected static bool SetImplementation<T>(LinkedList<T> recents, T implementation) where T : AbstractRuntimeApi
		{
			var find = recents.Find(implementation);
			if (find is not null)
				PromoteImplementation(recents, find);
			// else, a new implementation
			var node = recents.AddFirst(implementation);
			if (Settings.DisposeNotCurrentImplementation)
			{
				node.Next?.Value?.Dispose();
			}
			return true;
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

		/*
		#region support information
		/// <summary>
		/// When implemented by a derived class, get the supported <see cref="CombinationOfLocations"/> for all unary operations. Each value in the list can have any flags which indicate a support of a combination of certain memory locations. Or null if there are no unary operations.
		/// </summary>
		/// <remarks>Although the functionality of this property can be done by <see cref="SupportedNaryLocations(int)"/>, this one is specially separated for performance issues.</remarks>
		public abstract IReadOnlyList<CombinationOfLocations> SupportedUnaryLocations { get; }

		/// <summary>
		/// When implemented by a derived class, get list of the supported <see cref="CombinationOfLocations"/> for all binary operations. Each value in the list is a set of two values to indicate a supported pair of two certain (mixed) memory locations. Or null if there are no binary operations.
		/// </summary>
		/// <remarks>Although the functionality of this property can be done by <see cref="SupportedNaryLocations(int)"/>, this one is specially separated for performance issues.</remarks>
		public abstract IReadOnlyList<ImmutableTwoElementSet<CombinationOfLocations>> SupportedBinaryLocations { get; }

		/// <summary>
		/// When implemented by a derived class, get list of the supported <see cref="CombinationOfLocations"/> for all ternary operations. Each value in the list is a set of three values to indicate a supported triple of three certain (mixed) memory locations. Or null if there are no ternary operations.
		/// </summary>
		/// <remarks>Although the functionality of this property can be done by <see cref="SupportedNaryLocations(int)"/>, this one is specially separated for performance issues.</remarks>
		public abstract IReadOnlyList<ImmutableThreeElementSet<CombinationOfLocations>> SupportedTernaryLocations { get; }

		// Ignore Spelling: N-ary
		/// <summary>
		/// When implemented by a derived class, get list of the supported <see cref="CombinationOfLocations"/> for all N-ary operations. The default implementation assumes that there are not <paramref name="N"/>-ary operations with <paramref name="N"/> &gt; 3.
		/// </summary>
		/// <param name="N">The number of operands, must be <paramref name="N"/> &gt; 0</param>
		/// <returns>The list whose each value in the list is a set of <paramref name="N"/> values to indicate a supported combination of certain <see cref="CombinationOfLocations"/>. Or null if there are no N-ary operations.</returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="N"/> &lt;= 0</exception>
		public virtual IReadOnlyList<IImmutableSet<CombinationOfLocations>> SupportedNaryLocations(int N)
		{
			return N switch
			{
				1 => this.SupportedUnaryLocations.Select(l => (IImmutableSet<CombinationOfLocations>)(ImmutableZeroOneElementSet<CombinationOfLocations>)l),
				2 => this.SupportedBinaryLocations.Select(l => (IImmutableSet<CombinationOfLocations>)l),
				3 => this.SupportedTernaryLocations.Select(l => (IImmutableSet<CombinationOfLocations>)l),
				> 3 => Array.Empty<IImmutableSet<CombinationOfLocations>>(), // there are no N-ary operations
				_ => throw new ArgumentOutOfRangeException(nameof(N)),
			};
		}

		/// <summary>
		/// Check if the given <paramref name="location"/> is supported by unary operations of this <see cref="AbstractRuntimeApi"/> or not.
		/// </summary>
		/// <param name="location">The given <see cref="CombinationOfLocations"/></param>
		/// <returns>Whether <paramref name="location"/> is supported by this <see cref="AbstractRuntimeApi"/>.</returns>
		/// <remarks>Although the functionality of this method can be done by <see cref="IsSupportedNary(CombinationOfLocations[])"/>, this one is specially separated for performance issues.</remarks>
		public virtual bool IsSupportedUnitary(CombinationOfLocations location) => this.SupportedUnaryLocations.Contains(location);

		/// <summary>
		/// Check if the given <see cref="CombinationOfLocations"/>s are supported by binary operations of this <see cref="AbstractRuntimeApi"/> or not.
		/// </summary>
		/// <param name="location1">The first given <see cref="CombinationOfLocations"/></param>
		/// <param name="location2">The second given <see cref="CombinationOfLocations"/></param>
		/// <returns>Whether binary operations between <paramref name="location1"/> and <paramref name="location2"/> are supported by this <see cref="AbstractRuntimeApi"/>.</returns>
		/// <remarks>Although the functionality of this method can be done by <see cref="IsSupportedNary(CombinationOfLocations[])"/>, this one is specially separated for performance issues.</remarks>
		public virtual bool IsSupportedBinary(CombinationOfLocations location1, CombinationOfLocations location2)
			=> this.SupportedBinaryLocations.Contains((location1, location2));

		/// <summary>
		/// Check if the given <see cref="LocationType"/>s are supported by ternary operations of this <see cref="AbstractRuntimeApi"/> or not.
		/// </summary>
		/// <param name="location1">The first given <see cref="CombinationOfLocations"/></param>
		/// <param name="location2">The second given <see cref="CombinationOfLocations"/></param>
		/// <param name="location3">The third given <see cref="CombinationOfLocations"/></param>
		/// <returns>Whether ternary operations between <paramref name="location1"/> and <paramref name="location2"/> and <paramref name="location3"/> are supported by this <see cref="AbstractRuntimeApi"/>.</returns>
		/// <remarks>Although the functionality of this method can be done by <see cref="IsSupportedNary(CombinationOfLocations[])"/>, this one is specially separated for performance issues.</remarks>
		public virtual bool IsSupportedTernary(CombinationOfLocations location1, CombinationOfLocations location2, CombinationOfLocations location3)
			=> this.SupportedTernaryLocations.Contains((location1, location2, location3));

		/// <summary>
		/// Check if the given <paramref name="locations"/> are supported by N-ary operations of this <see cref="AbstractRuntimeApi"/> or not.
		/// </summary>
		/// <param name="locations">The given <see cref="CombinationOfLocations"/>s (must has exactly one or two flags)</param>
		/// <returns>Whether N-ary operations between <paramref name="locations"/> are supported by this <see cref="AbstractRuntimeApi"/>.</returns>
		public virtual bool IsSupportedNary(params CombinationOfLocations[] locations)
			=> this.SupportedNaryLocations(locations.Length).Contains(new ImmutableSet<CombinationOfLocations>(locations));
		#endregion
		*/
	}
}
