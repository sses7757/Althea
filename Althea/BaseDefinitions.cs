using System;
using System.Collections.Generic;

using Althea.Linq;
using Althea.Memory;


namespace Althea
{
	/// <summary>
	/// The base interface for all runtime APIs defined in this assembly
	/// </summary>
	/// <remarks>Typically, the <b>caller</b> are responsible for checking the input parameters of these methods.</remarks>
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
					throw new InvalidOperationException(Resource.CannotInitialize);
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
		/// Select the most recent implementation in <paramref name="recentApi"/> which fits a give <paramref name="pointer"/>
		/// </summary>
		/// <typeparam name="T">any API abstract class which implements <see cref="AbstractRuntimeApi"/></typeparam>
		/// <param name="recentApi">the <see cref="LinkedList{T}"/> to select in</param>
		/// <param name="pointer">the given <see cref="StoragePointer"/> to work with</param>
		/// <returns>The suitable most recent implementation or null if not found.</returns>
		/// <exception cref="ArgumentOutOfRangeException">if <paramref name="pointer"/> has a invalid <see cref="StoragePointer.Pointer"/> or <see cref="StoragePointer.LengthInBytes"/></exception>
		protected static T SelectImplementation<T>(LinkedList<T> recentApi, StoragePointer pointer) where T : AbstractRuntimeApi
		{
			// TODO: StoragePointer -> ICheckValidness ?
			if (pointer.Pointer == default || pointer.LengthInBytes == 0)
				throw new ArgumentOutOfRangeException(nameof(pointer), Resource.UnexpectedValue);
			if (recentApi.Count == 0)
				return null;
			var current = recentApi.First;
			while (current is not null)
			{
				if (current.Value.IsSupportedUnitary(pointer.Location.Location))
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
		/// Get the supported memory locations for all unary operations. Each flag in this value indicates a support of a certain location.
		/// </summary>
		public abstract StorageLocation SupportedUnaryLocations { get; }

		/// <summary>
		/// Get list of the supported memory locations for all binary operations. Each value must has exactly one or two flags to indicate a supported pair of certain locations.
		/// </summary>
		public abstract IReadOnlyList<StorageLocation> SupportedBinaryLocations { get; }

		/// <summary>
		/// Get list of the supported memory locations for all ternary operations. Each value must has exactly one to three flags to indicate a supported triple of certain locations.
		/// </summary>
		public abstract IReadOnlyList<StorageLocation> SupportedTernaryLocations { get; }

		// Ignore Spelling: N-ary
		/// <summary>
		/// Get list of the supported memory locations for all N-ary operations. Each value must has exactly one to three flags to indicate a supported triple of certain locations.
		/// </summary>
		/// <param name="N">the number of operands</param>
		/// <returns>The list of the supported memory locations for all N-ary operations.</returns>
		public virtual IReadOnlyList<StorageLocation> SupportedNaryLocations(int N)
		{
			return N switch
			{
				1 => new[] { this.SupportedUnaryLocations },
				2 => this.SupportedBinaryLocations,
				3 => this.SupportedTernaryLocations,
				_ => this.Direct_SupportedNaryLocations(N),
			};
		}

		/// <summary>
		/// Get list of the supported memory locations for all N-ary operations. This method will only be invoked internally with <paramref name="N"/> &gt; 3.
		/// </summary>
		/// <param name="N">the number of operands</param>
		/// <returns>The list of the supported memory locations for all N-ary operations.</returns>
		protected abstract IReadOnlyList<StorageLocation> Direct_SupportedNaryLocations(int N);

		/// <summary>
		/// Check if the given <paramref name="location"/> is supported by unary operations of this <see cref="AbstractRuntimeApi"/> or not.
		/// </summary>
		/// <param name="location">the given <see cref="StorageLocation"/> (can be combination of flags)</param>
		/// <returns>Whether <paramref name="location"/> is supported by this <see cref="AbstractRuntimeApi"/>.</returns>
		public virtual bool IsSupportedUnitary(StorageLocation location) => location != StorageLocation.Uri && (location & this.SupportedUnaryLocations) == location;

		/// <summary>
		/// Check if the given <paramref name="location"/> is supported by binary operations of this <see cref="AbstractRuntimeApi"/> or not.
		/// </summary>
		/// <param name="location">the given <see cref="StorageLocation"/> (must has exactly one or two flags)</param>
		/// <returns>Whether binary operations between <paramref name="location"/> are supported by this <see cref="AbstractRuntimeApi"/>.</returns>
		public virtual bool IsSupportedBinary(StorageLocation location) => location.NumberOfFlags() <= 2 && this.SupportedBinaryLocations.Contains(location);

		/// <summary>
		/// Check if the given <see cref="StorageLocation"/>s are supported by binary operations of this <see cref="AbstractRuntimeApi"/> or not.
		/// </summary>
		/// <param name="location1">the first given <see cref="StorageLocation"/> (must be a flag)</param>
		/// <param name="location2">the second given <see cref="StorageLocation"/> (must be a flag)</param>
		/// <returns>Whether binary operations between <paramref name="location1"/> and <paramref name="location2"/> are supported by this <see cref="AbstractRuntimeApi"/>.</returns>
		public virtual bool IsSupportedBinary(StorageLocation location1, StorageLocation location2)
			=> location1.IsFlag() && location2.IsFlag() && this.SupportedTernaryLocations.Contains(location1 | location2);

		/// <summary>
		/// Check if the given <paramref name="location"/> is supported by ternary operations of this <see cref="AbstractRuntimeApi"/> or not.
		/// </summary>
		/// <param name="location">the given <see cref="StorageLocation"/> (must has exactly one to three flags)</param>
		/// <returns>Whether ternary operations between <paramref name="location"/> are supported by this <see cref="AbstractRuntimeApi"/>.</returns>
		public virtual bool IsSupportedTernary(StorageLocation location) => location.NumberOfFlags() <= 3 && this.SupportedTernaryLocations.Contains(location);

		/// <summary>
		/// Check if the given <see cref="StorageLocation"/>s are supported by ternary operations of this <see cref="AbstractRuntimeApi"/> or not.
		/// </summary>
		/// <param name="location1">the first given <see cref="StorageLocation"/> (must be a flag)</param>
		/// <param name="location2">the second given <see cref="StorageLocation"/> (must be a flag)</param>
		/// <param name="location3">the third given <see cref="StorageLocation"/> (must be a flag)</param>
		/// <returns>Whether ternary operations between <paramref name="location1"/> and <paramref name="location2"/> and <paramref name="location3"/> are supported by this <see cref="AbstractRuntimeApi"/>.</returns>
		public virtual bool IsSupportedBinary(StorageLocation location1, StorageLocation location2, StorageLocation location3)
			=> location1.IsFlag() && location2.IsFlag() && location3.IsFlag() && this.SupportedTernaryLocations.Contains(location1 | location2 | location3);

		/// <summary>
		/// Check if the given <paramref name="location"/> is supported by N-ary operations of this <see cref="AbstractRuntimeApi"/> or not.
		/// </summary>
		/// <param name="location">the given <see cref="StorageLocation"/> (must has exactly one to N flags)</param>
		/// <returns>Whether N-ary operations between <paramref name="location"/> are supported by this <see cref="AbstractRuntimeApi"/>.</returns>
		public virtual bool IsSupportedNary(StorageLocation location) => this.SupportedNaryLocations(location.NumberOfFlags()).Contains(location);

		/// <summary>
		/// Check if the given <paramref name="locations"/> are supported by N-ary operations of this <see cref="AbstractRuntimeApi"/> or not.
		/// </summary>
		/// <param name="locations">the given <see cref="StorageLocation"/>s (must has exactly one or two flags)</param>
		/// <returns>Whether N-ary operations between <paramref name="locations"/> are supported by this <see cref="AbstractRuntimeApi"/>.</returns>
		public virtual bool IsSupportedNary(params StorageLocation[] locations)
			=> locations.All(l => l.IsFlag()) && this.SupportedTernaryLocations.Contains(locations.Aggregate((l1, l2) => l1 | l2, (StorageLocation)0));
		#endregion
	}
}
