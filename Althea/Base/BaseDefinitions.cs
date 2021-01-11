using System;
using System.Collections.Generic;

using Althea.Linq;
using Althea.Memory;
using Althea.Resources;


namespace Althea
{
	/// <summary>
	/// The base interface for all runtime APIs defined in this assembly
	/// </summary>
	/// <remarks>
	/// Typically, the <b>caller</b> are responsible for checking the input parameters of these methods. <br/>
	/// Typically, the inherited concrete classes shall perform lazy initializations if possible, because the instances created by default constructors will be held by the abstract API classes for a long time such that they can find the suitable candidates easily.
	/// </remarks>
	public abstract class AbstractRuntimeApi : IDisposable
	{
		#region static methods used for dispatching
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
					throw new InvalidOperationException(Backend.CannotInitialize);
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
		/// Promote the implementation in <paramref name="node"/> to the top of the linked list <paramref name="recentApi"/>
		/// </summary>
		/// <typeparam name="T">any API abstract class which implements <see cref="AbstractRuntimeApi"/></typeparam>
		/// <param name="recentApi">the <see cref="LinkedList{T}"/> to operate on</param>
		/// <param name="node">the <see cref="LinkedListNode{T}"/> to promote</param>
		protected static void PromoteImplementation<T>(LinkedList<T> recentApi, LinkedListNode<T> node) where T : AbstractRuntimeApi
		{
			if (recentApi is null || node is null)
				return;
			if (!recentApi.Contains(node.Value))
				return;
			recentApi.Remove(node.Value);
			recentApi.AddFirst(node.Value);
			Initialize(node);
		}

		/// <summary>
		/// Set the current implementation in <paramref name="recentApi"/> (the first node) to a given <paramref name="implementation"/>
		/// </summary>
		/// <typeparam name="T">any API abstract class which implements <see cref="AbstractRuntimeApi"/></typeparam>
		/// <param name="recentApi">the <see cref="LinkedList{T}"/> to operate on</param>
		/// <param name="implementation">the implementation indicated by a <see cref="Type"/></param>
		/// <returns>Success or not</returns>
		protected static bool SetImplementation<T>(LinkedList<T> recentApi, Type implementation) where T : AbstractRuntimeApi
		{
			if (implementation.IsGenericType || implementation.IsAbstract || !implementation.IsAssignableTo(typeof(T)))
			{
				return false;
			}
			// otherwise
			var current = recentApi.First;
			while (current is not null)
			{
				if (current.Value.GetType() == implementation)
				{
					PromoteImplementation(recentApi, current);
					return true;
				}
				current = current.Next;
			}
			// a new implementation
			recentApi.AddFirst(Create<T>(implementation));
			return true;
		}

		/// <summary>
		/// Select the most recent implementation in <paramref name="recentApi"/> which fits a given <paramref name="storage"/>
		/// </summary>
		/// <typeparam name="T">any API abstract class which implements <see cref="AbstractRuntimeApi"/></typeparam>
		/// <param name="recentApi">the <see cref="LinkedList{T}"/> to select in</param>
		/// <param name="storage">the given <see cref="IStorage"/> to work with</param>
		/// <returns>The suitable most recent implementation or null if not found.</returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="storage"/> has a invalid <see cref="StoragePointer.Pointer"/> or <see cref="StoragePointer.LengthInBytes"/></exception>
		/// <exception cref="InvalidOperationException">if there is no available back-end implementation or the suitable one cannot be initialized</exception>
		protected static T SelectImplementation<T>(LinkedList<T> recentApi, IStorage storage) where T : AbstractRuntimeApi
		{
			if (storage.IsValid())
				throw new ArgumentOutOfRangeException(nameof(storage), Parameter.UnexpectedValue);
			if (recentApi.Count == 0)
				throw new InvalidOperationException(Backend.CannotInitialize);
			var current = recentApi.First;
			if (current.Value.IsSupportedUnitary(storage.Location.Location))
			{
				return current.Value;
			}
			if (Helpers.Settings.UseRecentImplementation)
			{
				while ((current = current.Next) is not null)
				{
					Initialize(current);
					if (current.Value.IsSupportedUnitary(storage.Location.Location))
					{
						return current.Value;
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Select the most recent implementation in <paramref name="recentApi"/> which fits given <paramref name="pointer1"/> and <paramref name="pointer2"/>
		/// </summary>
		/// <typeparam name="T">any API abstract class which implements <see cref="AbstractRuntimeApi"/></typeparam>
		/// <param name="recentApi">the <see cref="LinkedList{T}"/> to select in</param>
		/// <param name="pointer1">the first given <see cref="StoragePointer"/> to work with</param>
		/// <param name="pointer2">the second given <see cref="StoragePointer"/> to work with</param>
		/// <returns>The suitable most recent implementation or null if not found.</returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="pointer1"/> <paramref name="pointer2"/> has a invalid <see cref="StoragePointer.Pointer"/> or <see cref="StoragePointer.LengthInBytes"/></exception>
		protected static T SelectImplementation<T>(LinkedList<T> recentApi, StoragePointer pointer1, StoragePointer pointer2) where T : AbstractRuntimeApi
		{
			if (pointer1.Pointer == default || pointer1.LengthInBytes == 0)
				throw new ArgumentOutOfRangeException(nameof(pointer1), Parameter.UnexpectedValue);
			if (pointer2.Pointer == default || pointer2.LengthInBytes == 0)
				throw new ArgumentOutOfRangeException(nameof(pointer2), Parameter.UnexpectedValue);
			if (recentApi.Count == 0)
				return null;
			var current = recentApi.First;
			while (current is not null)
			{
				Initialize(current);
				if (current.Value.IsSupportedBinary(pointer1.Location.Location, pointer2.Location.Location))
				{
					return current.Value;
				}
				current = current.Next;
			}
			return null;
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
		/// Get the supported memory locations for all unary operations. Each value in the list can have any flags which indicate a support of a combination of certain memory locations. Or null if there are no unary operations.
		/// </summary>
		public abstract IReadOnlyList<ImmutableZeroOneElementSet<MemoryLocation>> SupportedUnaryLocations { get; }

		/// <summary>
		/// Get list of the supported memory locations for all binary operations. Each value in the list is a set of two values to indicate a supported pair of two certain (mixed) memory locations. Or null if there are no binary operations.
		/// </summary>
		public abstract IReadOnlyList<ImmutableTwoElementSet<MemoryLocation>> SupportedBinaryLocations { get; }

		/// <summary>
		/// Get list of the supported memory locations for all ternary operations. Each value in the list is a set of three values to indicate a supported triple of three certain (mixed) memory locations. Or null if there are no ternary operations.
		/// </summary>
		public abstract IReadOnlyList<ImmutableThreeElementSet<MemoryLocation>> SupportedTernaryLocations { get; }

		// Ignore Spelling: N-ary
		/// <summary>
		/// Get list of the supported memory locations for all N-ary operations. Each in the list is a set of <paramref name="N"/> values to indicate a supported combination of certain (mixed) memory locations. Or null if there are no N-ary operations.
		/// </summary>
		/// <param name="N">the number of operands, must be <paramref name="N"/> &gt; 3</param>
		/// <returns>The list of the supported memory locations for all N-ary operations. Or null if there are no N-ary operations.</returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="N"/> &lt;= 3</exception>
		public abstract IReadOnlyList<IImmutableSet<MemoryLocation>> SupportedNaryLocations(int N);

		/// <summary>
		/// Check if the given <paramref name="location"/> is supported by unary operations of this <see cref="AbstractRuntimeApi"/> or not.
		/// </summary>
		/// <param name="location">the given <see cref="StorageLocation"/> (can be a combination of flags)</param>
		/// <returns>Whether <paramref name="location"/> is supported by this <see cref="AbstractRuntimeApi"/>.</returns>
		public virtual bool IsSupportedUnitary(MemoryLocation location) => this.SupportedUnaryLocations.Contains(location);

		/// <summary>
		/// Check if the given <see cref="MemoryLocation"/>s are supported by binary operations of this <see cref="AbstractRuntimeApi"/> or not.
		/// </summary>
		/// <param name="location1">the first given <see cref="MemoryLocation"/> (can be a combination of flags)</param>
		/// <param name="location2">the second given <see cref="MemoryLocation"/> (can be a combination of flags)</param>
		/// <returns>Whether binary operations between <paramref name="location1"/> and <paramref name="location2"/> are supported by this <see cref="AbstractRuntimeApi"/>.</returns>
		public virtual bool IsSupportedBinary(MemoryLocation location1, MemoryLocation location2)
			=> this.SupportedBinaryLocations.Contains((location1, location2));

		/// <summary>
		/// Check if the given <see cref="MemoryLocation"/>s are supported by ternary operations of this <see cref="AbstractRuntimeApi"/> or not.
		/// </summary>
		/// <param name="location1">the first given <see cref="MemoryLocation"/> (must be a flag)</param>
		/// <param name="location2">the second given <see cref="MemoryLocation"/> (must be a flag)</param>
		/// <param name="location3">the third given <see cref="MemoryLocation"/> (must be a flag)</param>
		/// <returns>Whether ternary operations between <paramref name="location1"/> and <paramref name="location2"/> and <paramref name="location3"/> are supported by this <see cref="AbstractRuntimeApi"/>.</returns>
		public virtual bool IsSupportedBinary(MemoryLocation location1, MemoryLocation location2, MemoryLocation location3)
			=> this.SupportedTernaryLocations.Contains((location1, location2, location3));

		/// <summary>
		/// Check if the given <paramref name="locations"/> are supported by N-ary operations of this <see cref="AbstractRuntimeApi"/> or not.
		/// </summary>
		/// <param name="locations">the given <see cref="MemoryLocation"/>s (must has exactly one or two flags)</param>
		/// <returns>Whether N-ary operations between <paramref name="locations"/> are supported by this <see cref="AbstractRuntimeApi"/>.</returns>
		public virtual bool IsSupportedNary(params MemoryLocation[] locations)
			=> this.SupportedNaryLocations(locations.Length).Contains(new ImmutableSet<MemoryLocation>(locations));
		#endregion
	}
}
