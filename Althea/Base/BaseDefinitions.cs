using System;
using System.Collections.Generic;

using Althea.Linq;


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
				return null;
			}
			var constructor = type.GetConstructor(Array.Empty<Type>());
			if (constructor is not null)
			{
				return constructor.Invoke(Array.Empty<object>()) as T;
			}
			else
			{
				var constructors = type.GetConstructors();
				if (constructors.Length == 0 || !constructors.Contains(0, c => c.GetParameters().Length))
					throw new InvalidOperationException(Resources.Backend.CannotInitialize);
				return constructors.Where(c => c.GetParameters().Length == 0)[0].Invoke(Array.Empty<object>()) as T;
			}
		}

		/// <summary>
		/// Initialize a <see cref="LinkedListNode{T}"/> of <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">any API abstract class which implements <see cref="AbstractRuntimeApi"/></typeparam>
		/// <param name="node">the <see cref="LinkedListNode{T}"/> to initialize</param>
		protected static void Initialize<T>(LinkedListNode<T> node) where T : AbstractRuntimeApi
		{
			if (node.Value is not null && !node.Value.Disposed)
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
			if (Helpers.Settings.DisposeNotCurrentImplementation)
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
			if (Helpers.Settings.DisposeNotCurrentImplementation)
			{
				node.Next?.Value?.Dispose();
			}
			return true;
		}
		#endregion

		#region static methods used for dispatching
		private static void Check<T>(LinkedList<T> recents, params IStorage[] storages) where T : AbstractRuntimeApi
		{
			if (recents.Count == 0)
				throw new InvalidOperationException(Resources.Backend.NotAvailable);
			if (storages.Any(s => !s.IsValid()))
				throw new ArgumentOutOfRangeException(nameof(storages), Resources.Parameter.InvalidValue);
			if (Helpers.Settings.DisposeNotCurrentImplementation)
			{
				// dispose all implementations not using
				var node = recents.First;
				while (node is not null)
				{
					node.Value?.Dispose();
					node = node.Next;
				}
			}
		}

		/// <summary>
		/// Select the most recent implementation in <paramref name="recents"/> which fits a given <paramref name="storage"/>
		/// </summary>
		/// <typeparam name="T">any API abstract class which implements <see cref="AbstractRuntimeApi"/></typeparam>
		/// <param name="recents">The <see cref="LinkedList{T}"/> of recent APIs to select in.</param>
		/// <param name="storage">The given <see cref="IStorage"/> to work with.</param>
		/// <returns>The suitable most recent implementation or null if not found.</returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="storage"/> has invalid value(s), such as <see cref="StoragePointer.Pointer"/> and <see cref="StoragePointer.LengthInBytes"/></exception>
		/// <exception cref="InvalidOperationException">if there is no available back-end implementation or the suitable one cannot be initialized</exception>
		protected static T SelectImplementation<T>(LinkedList<T> recents, IStorage storage) where T : AbstractRuntimeApi
		{
			Check(recents, storage);

			var current = recents.First;
			if (current.Value.IsSupportedUnitary(storage.LocationsCombination))
			{
				return current.Value;
			}
			while ((current = current.Next) is not null)
			{
				Initialize(current);
				if (current.Value.IsSupportedUnitary(storage.LocationsCombination))
				{
					return current.Value;
				}
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
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="storage1"/> or <paramref name="storage2"/> has invalid value(s), such as <see cref="StoragePointer.Pointer"/> and <see cref="StoragePointer.LengthInBytes"/></exception>
		/// <exception cref="InvalidOperationException">if there is no available back-end implementation or the suitable one cannot be initialized</exception>
		protected static T SelectImplementation<T>(LinkedList<T> recents, IStorage storage1, IStorage storage2) where T : AbstractRuntimeApi
		{
			Check(recents, storage1, storage2);

			var current = recents.First;
			if (current.Value.IsSupportedBinary(storage1.LocationsCombination, storage2.LocationsCombination))
			{
				return current.Value;
			}
			while ((current = current.Next) is not null)
			{
				Initialize(current);
				if (current.Value.IsSupportedBinary(storage1.LocationsCombination, storage2.LocationsCombination))
				{
					return current.Value;
				}
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
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="storage1"/> or <paramref name="storage2"/> or <paramref name="storage3"/> has invalid value(s), such as <see cref="StoragePointer.Pointer"/> and <see cref="StoragePointer.LengthInBytes"/></exception>
		/// <exception cref="InvalidOperationException">if there is no available back-end implementation or the suitable one cannot be initialized</exception>
		protected static T SelectImplementation<T>(LinkedList<T> recents, IStorage storage1, IStorage storage2, IStorage storage3) where T : AbstractRuntimeApi
		{
			Check(recents, storage1, storage2, storage3);

			var current = recents.First;
			if (current.Value.IsSupportedTernary(storage1.LocationsCombination, storage2.LocationsCombination, storage3.LocationsCombination))
			{
				return current.Value;
			}
			while ((current = current.Next) is not null)
			{
				Initialize(current);
				if (current.Value.IsSupportedTernary(storage1.LocationsCombination, storage2.LocationsCombination, storage3.LocationsCombination))
				{
					return current.Value;
				}
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
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="storages"/> contains one <see cref="IStorage"/> with invalid value(s), such as <see cref="StoragePointer.Pointer"/> and <see cref="StoragePointer.LengthInBytes"/></exception>
		/// <exception cref="InvalidOperationException">if there is no available back-end implementation or the suitable one cannot be initialized</exception>
		protected static T SelectImplementation<T>(LinkedList<T> recents, params IStorage[] storages) where T : AbstractRuntimeApi
		{
			Check(recents, storages);

			var current = recents.First;
			if (current.Value.IsSupportedNary(storages.Select(s => s.LocationsCombination).ToArray()))
			{
				return current.Value;
			}
			while ((current = current.Next) is not null)
			{
				Initialize(current);
				if (current.Value.IsSupportedNary(storages.Select(s => s.LocationsCombination).ToArray()))
				{
					return current.Value;
				}
			}
			throw new InvalidOperationException(Resources.Backend.NotAvailable);
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
		/// Get the supported locations for all unary operations. Each value in the list can have any flags which indicate a support of a combination of certain memory locations. Or null if there are no unary operations.
		/// </summary>
		public abstract IReadOnlyList<StorageDetailsCombination> SupportedUnaryLocations { get; }

		/// <summary>
		/// Get list of the supported locations for all binary operations. Each value in the list is a set of two values to indicate a supported pair of two certain (mixed) memory locations. Or null if there are no binary operations.
		/// </summary>
		public abstract IReadOnlyList<ImmutableTwoElementSet<StorageDetailsCombination>> SupportedBinaryLocations { get; }

		/// <summary>
		/// Get list of the supported locations for all ternary operations. Each value in the list is a set of three values to indicate a supported triple of three certain (mixed) memory locations. Or null if there are no ternary operations.
		/// </summary>
		public abstract IReadOnlyList<ImmutableThreeElementSet<StorageDetailsCombination>> SupportedTernaryLocations { get; }

		// Ignore Spelling: N-ary
		/// <summary>
		/// Get list of the supported locations for all N-ary operations. Each in the list is a set of <paramref name="N"/> values to indicate a supported combination of certain (mixed) memory locations. Or null if there are no N-ary operations.
		/// </summary>
		/// <param name="N">the number of operands, must be <paramref name="N"/> &gt; 3</param>
		/// <returns>The list of the supported locations for all N-ary operations. Or null if there are no N-ary operations.</returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="N"/> &lt;= 3</exception>
		public abstract IReadOnlyList<IImmutableSet<StorageDetailsCombination>> SupportedNaryLocations(int N);

		/// <summary>
		/// Check if the given <paramref name="location"/> is supported by unary operations of this <see cref="AbstractRuntimeApi"/> or not.
		/// </summary>
		/// <param name="location">the given <see cref="StorageDetailsCombination"/></param>
		/// <returns>Whether <paramref name="location"/> is supported by this <see cref="AbstractRuntimeApi"/>.</returns>
		public virtual bool IsSupportedUnitary(StorageDetailsCombination location) => this.SupportedUnaryLocations.Contains(location);

		/// <summary>
		/// Check if the given <see cref="StorageDetailsCombination"/>s are supported by binary operations of this <see cref="AbstractRuntimeApi"/> or not.
		/// </summary>
		/// <param name="location1">the first given <see cref="StorageDetailsCombination"/></param>
		/// <param name="location2">the second given <see cref="StorageDetailsCombination"/></param>
		/// <returns>Whether binary operations between <paramref name="location1"/> and <paramref name="location2"/> are supported by this <see cref="AbstractRuntimeApi"/>.</returns>
		public virtual bool IsSupportedBinary(StorageDetailsCombination location1, StorageDetailsCombination location2)
			=> this.SupportedBinaryLocations.Contains((location1, location2));

		/// <summary>
		/// Check if the given <see cref="StorageLocation"/>s are supported by ternary operations of this <see cref="AbstractRuntimeApi"/> or not.
		/// </summary>
		/// <param name="location1">the first given <see cref="StorageDetailsCombination"/></param>
		/// <param name="location2">the second given <see cref="StorageDetailsCombination"/></param>
		/// <param name="location3">the third given <see cref="StorageDetailsCombination"/></param>
		/// <returns>Whether ternary operations between <paramref name="location1"/> and <paramref name="location2"/> and <paramref name="location3"/> are supported by this <see cref="AbstractRuntimeApi"/>.</returns>
		public virtual bool IsSupportedTernary(StorageDetailsCombination location1, StorageDetailsCombination location2, StorageDetailsCombination location3)
			=> this.SupportedTernaryLocations.Contains((location1, location2, location3));

		/// <summary>
		/// Check if the given <paramref name="locations"/> are supported by N-ary operations of this <see cref="AbstractRuntimeApi"/> or not.
		/// </summary>
		/// <param name="locations">the given <see cref="StorageDetailsCombination"/>s (must has exactly one or two flags)</param>
		/// <returns>Whether N-ary operations between <paramref name="locations"/> are supported by this <see cref="AbstractRuntimeApi"/>.</returns>
		public virtual bool IsSupportedNary(params StorageDetailsCombination[] locations)
			=> this.SupportedNaryLocations(locations.Length).Contains(new ImmutableSet<StorageDetailsCombination>(locations));
		#endregion
	}
}
