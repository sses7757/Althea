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
		/// <param name="type">the <see cref="Type"/> (not the type of used to initialize</param>
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
		/// <param name="node">the <see cref="LinkedListNode{T}"/> to initialize</param>
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
		/// <param name="recents">the <see cref="LinkedList{T}"/> of recent APIs to operate on</param>
		/// <param name="node">the <see cref="LinkedListNode{T}"/> to promote</param>
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
		/// <param name="recents">the <see cref="LinkedList{T}"/> of recent APIs to operate on</param>
		/// <param name="implementation">the implementation indicated by a <see cref="Type"/></param>
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
		#endregion

		#region static methods used for dispatching
		/// <summary>
		/// Dispose all implementations in recent API list (<paramref name="recents"/>) which are not currently using
		/// </summary>
		/// <param name="recents">The recent API list as a <see cref="LinkedList{T}"/></param>
		/// <remarks>
		/// If <see cref="Helpers.Settings.DisposeNotCurrentImplementation"/> is false, this method does nothing.<br/>
		/// It is recommended for all implemented API functions to call this method at the end of the procedure. Of course, it can be done manually or by tools which automatically edit them.
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void DisposeNotCurrent<T>(LinkedList<T> recents) where T : AbstractRuntimeApi
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
			if (recents.Count == 0)
				throw new InvalidOperationException(Resources.Backend.NotAvailable);
			DisposeNotCurrent(recents);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void Check<T>(LinkedList<T> recents, IStorage storage1) where T : AbstractRuntimeApi
		{
			if (storage1.IsValid())
				throw new ArgumentNullException(nameof(storage1));
			Check(recents);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void Check<T>(LinkedList<T> recents, IStorage storage1, IStorage storage2) where T : AbstractRuntimeApi
		{
			if (storage1.IsValid())
				throw new ArgumentNullException(nameof(storage1));
			if (storage2.IsValid())
				throw new ArgumentNullException(nameof(storage2));
			Check(recents);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void Check<T>(LinkedList<T> recents, IStorage storage1, IStorage storage2, IStorage storage3) where T : AbstractRuntimeApi
		{
			if (storage1.IsValid())
				throw new ArgumentNullException(nameof(storage1));
			if (storage2.IsValid())
				throw new ArgumentNullException(nameof(storage2));
			if (storage3.IsValid())
				throw new ArgumentNullException(nameof(storage3));
			Check(recents);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void Check<T>(LinkedList<T> recents, params IStorage[] storages) where T : AbstractRuntimeApi
		{
			if (recents.Count == 0)
				throw new InvalidOperationException(Resources.Backend.NotAvailable);
			if (storages.Any(s => !s.IsValid()))
				throw new ArgumentNullException(nameof(storages));
			Check(recents);
		}

		/// <summary>
		/// Select the most recent implementation in <paramref name="recents"/> which fits a given <paramref name="location"/>
		/// </summary>
		/// <typeparam name="T">any API abstract class which implements <see cref="AbstractRuntimeApi"/></typeparam>
		/// <param name="recents">The <see cref="LinkedList{T}"/> of recent APIs to select in.</param>
		/// <param name="location">The given <see cref="StorageLocation"/> to work with.</param>
		/// <returns>The suitable most recent implementation or null if not found.</returns>
		/// <exception cref="InvalidOperationException">if there is no available back-end implementation or the suitable one cannot be initialized</exception>
		/// <remarks>Although the functionality of this method can be done by <see cref="SelectImplementation{T}(LinkedList{T}, IStorage[])"/>, this method is specially separated for performance issues.</remarks>
		protected static T SelectImplementation<T>(LinkedList<T> recents, StorageLocation location) where T : AbstractRuntimeApi
		{
			Check(recents);

			var current = recents.First;
			if (current is null)
				throw new InvalidOperationException(Resources.Backend.NotAvailable);
			if (current.Value.IsSupportedUnitary(location))
			{
				return current.Value;
			}
			current = current.Next;
			while (current is not null)
			{
				Initialize(current);
				if (current.Value.IsSupportedUnitary(location))
				{
					return current.Value;
				}
				current = current.Next;
			}
			throw new InvalidOperationException(Resources.Backend.NotAvailable);
		}


		/// <summary>
		/// Select the most recent implementation in <paramref name="recents"/> which fits a given <paramref name="storage"/>
		/// </summary>
		/// <typeparam name="T">any API abstract class which implements <see cref="AbstractRuntimeApi"/></typeparam>
		/// <param name="recents">The <see cref="LinkedList{T}"/> of recent APIs to select in.</param>
		/// <param name="storage">The given <see cref="IStorage"/> to work with.</param>
		/// <returns>The suitable most recent implementation or null if not found.</returns>
		/// <exception cref="ArgumentNullException">if <paramref name="storage"/> has invalid value(s), such as <see cref="PointerSegment.Pointer"/> and <see cref="PointerSegment.LengthInBytes"/></exception>
		/// <exception cref="InvalidOperationException">if there is no available back-end implementation or the suitable one cannot be initialized</exception>
		/// <remarks>Although the functionality of this method can be done by <see cref="SelectImplementation{T}(LinkedList{T}, IStorage[])"/>, this method is specially separated for performance issues.</remarks>
		protected static T SelectImplementation<T>(LinkedList<T> recents, IStorage storage) where T : AbstractRuntimeApi
		{
			Check(recents, storage);

			var current = recents.First;
			if (current is null)
				throw new InvalidOperationException(Resources.Backend.NotAvailable);
			if (current.Value.IsSupportedUnitary(storage.LocationDescription))
			{
				return current.Value;
			}
			current = current.Next;
			while (current is not null)
			{
				Initialize(current);
				if (current.Value.IsSupportedUnitary(storage.LocationDescription))
				{
					return current.Value;
				}
				current = current.Next;
			}
			throw new InvalidOperationException(Resources.Backend.NotAvailable);
		}

		/// <summary>
		/// Select the most recent implementation in <paramref name="recents"/> which fits given <see cref="IStorage"/>s
		/// </summary>
		/// <typeparam name="T">any API abstract class which implements <see cref="AbstractRuntimeApi"/></typeparam>
		/// <param name="recents">The <see cref="LinkedList{T}"/> of recent APIs to select in.</param>
		/// <param name="storage1">The first given <see cref="IStorage"/> to work with.</param>
		/// <param name="storage2">The second given <see cref="IStorage"/> to work with.</param>
		/// <returns>The suitable most recent implementation or null if not found.</returns>
		/// <exception cref="ArgumentNullException">if <paramref name="storage1"/> or <paramref name="storage2"/> has invalid value(s), such as <see cref="PointerSegment.Pointer"/> and <see cref="PointerSegment.LengthInBytes"/></exception>
		/// <exception cref="InvalidOperationException">if there is no available back-end implementation or the suitable one cannot be initialized</exception>
		/// <remarks>Although the functionality of this method can be done by <see cref="SelectImplementation{T}(LinkedList{T}, IStorage[])"/>, this method is specially separated for performance issues.</remarks>
		protected static T SelectImplementation<T>(LinkedList<T> recents, IStorage storage1, IStorage storage2) where T : AbstractRuntimeApi
		{
			Check(recents, storage1, storage2);

			var current = recents.First;
			if (current is null)
				throw new InvalidOperationException(Resources.Backend.NotAvailable);
			if (current.Value.IsSupportedBinary(storage1.LocationDescription, storage2.LocationDescription))
			{
				return current.Value;
			}
			current = current.Next;
			while (current is not null)
			{
				Initialize(current);
				if (current.Value.IsSupportedBinary(storage1.LocationDescription, storage2.LocationDescription))
				{
					return current.Value;
				}
				current = current.Next;
			}
			throw new InvalidOperationException(Resources.Backend.NotAvailable);
		}

		/// <summary>
		/// Select the most recent implementation in <paramref name="recents"/> which fits given <see cref="IStorage"/>s
		/// </summary>
		/// <typeparam name="T">any API abstract class which implements <see cref="AbstractRuntimeApi"/></typeparam>
		/// <param name="recents">The <see cref="LinkedList{T}"/> of recent APIs to select in.</param>
		/// <param name="storage1">The first given <see cref="IStorage"/> to work with.</param>
		/// <param name="storage2">The second given <see cref="IStorage"/> to work with.</param>
		/// <param name="storage3">The third given <see cref="IStorage"/> to work with.</param>
		/// <returns>The suitable most recent implementation or null if not found.</returns>
		/// <exception cref="ArgumentNullException">if <paramref name="storage1"/> or <paramref name="storage2"/> or <paramref name="storage3"/> has invalid value(s), such as <see cref="PointerSegment.Pointer"/> and <see cref="PointerSegment.LengthInBytes"/></exception>
		/// <exception cref="InvalidOperationException">if there is no available back-end implementation or the suitable one cannot be initialized</exception>
		/// <remarks>Although the functionality of this method can be done by <see cref="SelectImplementation{T}(LinkedList{T}, IStorage[])"/>, this method is specially separated for performance issues.</remarks>
		protected static T SelectImplementation<T>(LinkedList<T> recents, IStorage storage1, IStorage storage2, IStorage storage3) where T : AbstractRuntimeApi
		{
			Check(recents, storage1, storage2, storage3);

			var current = recents.First;
			if (current is null)
				throw new InvalidOperationException(Resources.Backend.NotAvailable);
			if (current.Value.IsSupportedTernary(storage1.LocationDescription, storage2.LocationDescription, storage3.LocationDescription))
			{
				return current.Value;
			}
			current = current.Next;
			while (current is not null)
			{
				Initialize(current);
				if (current.Value.IsSupportedTernary(storage1.LocationDescription, storage2.LocationDescription, storage3.LocationDescription))
				{
					return current.Value;
				}
				current = current.Next;
			}
			throw new InvalidOperationException(Resources.Backend.NotAvailable);
		}

		/// <summary>
		/// Select the most recent implementation in <paramref name="recents"/> which fits given <paramref name="storages"/>
		/// </summary>
		/// <typeparam name="T">any API abstract class which implements <see cref="AbstractRuntimeApi"/></typeparam>
		/// <param name="recents">The <see cref="LinkedList{T}"/> of recent APIs to select in.</param>
		/// <param name="storages">The given <see cref="IStorage"/>s to work with.</param>
		/// <returns>The suitable most recent implementation or null if not found.</returns>
		/// <exception cref="ArgumentNullException">if <paramref name="storages"/> contains one <see cref="IStorage"/> with invalid value(s), such as <see cref="PointerSegment.Pointer"/> and <see cref="PointerSegment.LengthInBytes"/></exception>
		/// <exception cref="InvalidOperationException">if there is no available back-end implementation or the suitable one cannot be initialized</exception>
		protected static T SelectImplementation<T>(LinkedList<T> recents, params IStorage[] storages) where T : AbstractRuntimeApi
		{
			Check(recents, storages);

			var current = recents.First;
			if (current is null)
				throw new InvalidOperationException(Resources.Backend.NotAvailable);
			if (current.Value.IsSupportedNary(storages.Select(s => s.LocationDescription).ToArray()))
			{
				return current.Value;
			}
			current = current.Next;
			while (current is not null)
			{
				Initialize(current);
				if (current.Value.IsSupportedNary(storages.Select(s => s.LocationDescription).ToArray()))
				{
					return current.Value;
				}
				current = current.Next;
			}
			throw new InvalidOperationException(Resources.Backend.NotAvailable);
		}
		#endregion

		#region static methods used for support information
		/// <summary>
		/// Generate a list of unary <see cref="CombinationOfLocations"/>s from given "pure" <see cref="StorageLocation"/>s by enumerating all possible combinations of all possible lengths
		/// </summary>
		/// <param name="locations">The given "pure" <see cref="StorageLocation"/>s</param>
		/// <returns>The generated list of <see cref="CombinationOfLocations"/>s, or null if <paramref name="locations"/> is null or empty</returns>
		/// <exception cref="OverflowException">If the length of <paramref name="locations"/> is larger than 64</exception>
		/// <remarks>Although the functionality of this method can be done by <see cref="GenerateNaryLoactions"/>, this one is specially separated for performance issues.</remarks>
		protected static IReadOnlyList<CombinationOfLocations> GenerateUnaryLoactions(params StorageLocation[] locations)
		{
			if (locations is null || locations.Length == 0)
				return Array.Empty<CombinationOfLocations>();

			byte length = checked((byte)locations.Length);
			ulong max = checked(1UL << length);
			CombinationOfLocations[] combinations = new CombinationOfLocations[max - 1];
			Span<StorageLocation> combination = stackalloc StorageLocation[locations.Length];
			for (ulong i = 0; i < max; i++)
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
		protected static IReadOnlyList<ImmutableTwoElementSet<CombinationOfLocations>> GenerateBinaryLoactions(params StorageLocation[] locations)
		{
			if (locations is null || locations.Length < 2)
				return Array.Empty<ImmutableTwoElementSet<CombinationOfLocations>>();

			long unaryLength = locations.LongLength;
			ulong max = (ulong)unaryLength * (ulong)(unaryLength + 1) / 2; // binomial of (unaryLength + 1, 2)
			var combinations = new ImmutableTwoElementSet<CombinationOfLocations>[max];
			var unary = (CombinationOfLocations[])GenerateUnaryLoactions(locations);
			ulong n = 0;
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
		protected static IReadOnlyList<ImmutableThreeElementSet<CombinationOfLocations>> GenerateTernaryLoactions(params StorageLocation[] locations)
		{
			if (locations is null || locations.Length < 3)
				return Array.Empty<ImmutableThreeElementSet<CombinationOfLocations>>();

			int unaryLength = locations.Length;
			ulong max = 3U.CombinationNumber(2 + (uint)unaryLength); // binomial (unaryLength + 2, 3)
			var combinations = new ImmutableThreeElementSet<CombinationOfLocations>[max];
			var unary = (CombinationOfLocations[])GenerateUnaryLoactions(locations);
			ulong n = 0;
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

		// Ignore Spelling: N-arys
		/// <summary>
		/// Generate a list of N-ary (<see cref="CombinationOfLocations"/>1, ..., <see cref="CombinationOfLocations"/><paramref name="N"/>)s from given "pure" <see cref="StorageLocation"/>s by enumerating all possible combinations of all possible lengths
		/// </summary>
		/// <param name="N">The number of operands, must be positive</param>
		/// <param name="locations">The given "pure" <see cref="StorageLocation"/>s</param>
		/// <returns>The generated list of <see cref="CombinationOfLocations"/>'s N-arys; null if <paramref name="locations"/> is null, or <paramref name="locations"/> has less than <paramref name="N"/> elements</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="N"/> is not a positive number</exception>
		/// <exception cref="OverflowException">If the length of <paramref name="locations"/> is larger than 64 / <paramref name="N"/></exception>
		protected static IReadOnlyList<IImmutableSet<CombinationOfLocations>> GenerateNaryLoactions(int N, params StorageLocation[] locations)
		{
			if (N <= 0)
				throw new ArgumentOutOfRangeException(nameof(N), Resources.Parameter.MustPositive);
			if (locations is null || locations.Length < N)
				return Array.Empty<IImmutableSet<CombinationOfLocations>>();

			int unaryLength = locations.Length;
			ulong max = ((uint)N).CombinationNumber((uint)(N + unaryLength - 1)); // binomial (unaryLength + N - 1, N)
			var combinations = new IImmutableSet<CombinationOfLocations>[max];
			var unary = (CombinationOfLocations[])GenerateUnaryLoactions(locations);
			Span<int> indices = stackalloc int[N];
			for (ulong n = 0; n < max; n++)
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

		#region support information
		/// <summary>
		/// Get the supported <see cref="CombinationOfLocations"/> for all unary operations. Each value in the list can have any flags which indicate a support of a combination of certain memory locations. Or null if there are no unary operations.
		/// </summary>
		/// <remarks>Although the functionality of this property can be done by <see cref="SupportedNaryLocations(int)"/>, this one is specially separated for performance issues.</remarks>
		public abstract IReadOnlyList<CombinationOfLocations> SupportedUnaryLocations { get; }

		/// <summary>
		/// Get list of the supported <see cref="CombinationOfLocations"/> for all binary operations. Each value in the list is a set of two values to indicate a supported pair of two certain (mixed) memory locations. Or null if there are no binary operations.
		/// </summary>
		/// <remarks>Although the functionality of this property can be done by <see cref="SupportedNaryLocations(int)"/>, this one is specially separated for performance issues.</remarks>
		public abstract IReadOnlyList<ImmutableTwoElementSet<CombinationOfLocations>> SupportedBinaryLocations { get; }

		/// <summary>
		/// Get list of the supported <see cref="CombinationOfLocations"/> for all ternary operations. Each value in the list is a set of three values to indicate a supported triple of three certain (mixed) memory locations. Or null if there are no ternary operations.
		/// </summary>
		/// <remarks>Although the functionality of this property can be done by <see cref="SupportedNaryLocations(int)"/>, this one is specially separated for performance issues.</remarks>
		public abstract IReadOnlyList<ImmutableThreeElementSet<CombinationOfLocations>> SupportedTernaryLocations { get; }

		// Ignore Spelling: N-ary
		/// <summary>
		/// Get list of the supported <see cref="CombinationOfLocations"/> for all N-ary operations. Each in the list is a set of <paramref name="N"/> values to indicate a supported combination of certain <see cref="CombinationOfLocations"/>. Or null if there are no N-ary operations.
		/// </summary>
		/// <param name="N">the number of operands, must be <paramref name="N"/> &gt; 0</param>
		/// <returns>The list of the supported locations for all N-ary operations. Or null if there are no N-ary operations.</returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="N"/> &lt;= 0</exception>
		public abstract IReadOnlyList<IImmutableSet<CombinationOfLocations>> SupportedNaryLocations(int N);

		/// <summary>
		/// Check if the given <paramref name="location"/> is supported by unary operations of this <see cref="AbstractRuntimeApi"/> or not.
		/// </summary>
		/// <param name="location">the given <see cref="CombinationOfLocations"/></param>
		/// <returns>Whether <paramref name="location"/> is supported by this <see cref="AbstractRuntimeApi"/>.</returns>
		/// <remarks>Although the functionality of this method can be done by <see cref="IsSupportedNary(CombinationOfLocations[])"/>, this one is specially separated for performance issues.</remarks>
		public virtual bool IsSupportedUnitary(CombinationOfLocations location) => this.SupportedUnaryLocations.Contains(location);

		/// <summary>
		/// Check if the given <see cref="CombinationOfLocations"/>s are supported by binary operations of this <see cref="AbstractRuntimeApi"/> or not.
		/// </summary>
		/// <param name="location1">the first given <see cref="CombinationOfLocations"/></param>
		/// <param name="location2">the second given <see cref="CombinationOfLocations"/></param>
		/// <returns>Whether binary operations between <paramref name="location1"/> and <paramref name="location2"/> are supported by this <see cref="AbstractRuntimeApi"/>.</returns>
		/// <remarks>Although the functionality of this method can be done by <see cref="IsSupportedNary(CombinationOfLocations[])"/>, this one is specially separated for performance issues.</remarks>
		public virtual bool IsSupportedBinary(CombinationOfLocations location1, CombinationOfLocations location2)
			=> this.SupportedBinaryLocations.Contains((location1, location2));

		/// <summary>
		/// Check if the given <see cref="LocationType"/>s are supported by ternary operations of this <see cref="AbstractRuntimeApi"/> or not.
		/// </summary>
		/// <param name="location1">the first given <see cref="CombinationOfLocations"/></param>
		/// <param name="location2">the second given <see cref="CombinationOfLocations"/></param>
		/// <param name="location3">the third given <see cref="CombinationOfLocations"/></param>
		/// <returns>Whether ternary operations between <paramref name="location1"/> and <paramref name="location2"/> and <paramref name="location3"/> are supported by this <see cref="AbstractRuntimeApi"/>.</returns>
		/// <remarks>Although the functionality of this method can be done by <see cref="IsSupportedNary(CombinationOfLocations[])"/>, this one is specially separated for performance issues.</remarks>
		public virtual bool IsSupportedTernary(CombinationOfLocations location1, CombinationOfLocations location2, CombinationOfLocations location3)
			=> this.SupportedTernaryLocations.Contains((location1, location2, location3));

		/// <summary>
		/// Check if the given <paramref name="locations"/> are supported by N-ary operations of this <see cref="AbstractRuntimeApi"/> or not.
		/// </summary>
		/// <param name="locations">the given <see cref="CombinationOfLocations"/>s (must has exactly one or two flags)</param>
		/// <returns>Whether N-ary operations between <paramref name="locations"/> are supported by this <see cref="AbstractRuntimeApi"/>.</returns>
		public virtual bool IsSupportedNary(params CombinationOfLocations[] locations)
			=> this.SupportedNaryLocations(locations.Length).Contains(new ImmutableSet<CombinationOfLocations>(locations));
		#endregion
	}
}
