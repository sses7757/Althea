using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;

using Althea.Helpers;
using Althea.Linq;


namespace Althea
{
	/// <summary>
	/// The base abstract class for all runtime API classes defined in and out of this assembly. Any specific API abstract class shall inherit this class. (Concrete classes shall NOT inherit this one directly.)<br/>
	/// The derived concrete class(es) shall be able to serialized and deserialized by <see cref="JsonSerializer"/> (typically through indicating <see cref="JsonConstructorAttribute"/> and implementing <see cref="CurrentConverter"/>).
	/// </summary>
	/// <typeparam name="TApi">Any API abstract class which inherits <see cref="AbstractRuntimeApi{TApi}"/>, like <see cref="Storage.AbstractApi"/></typeparam>
	/// <remarks>
	/// Typically, the <b>callers</b> are responsible for checking the input parameters of all the defined API methods.
	/// </remarks>
	public abstract class AbstractRuntimeApi<TApi> : IDisposable where TApi : AbstractRuntimeApi<TApi>
	{
		#region basic
		#region generic static variables for each sub class
		/// <summary>
		/// The recently used APIs (of type <typeparamref name="TApi"/>)
		/// </summary>
		protected static readonly LinkedList<TApi> RecentAPIs = new();
		#endregion

		#region static methods used for creating API class instances
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static TApi Create(Type type)
		{
			if (type.IsGenericType || type.IsAbstract || !type.IsAssignableTo(typeof(TApi)))
			{
				throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(type));
			}
			var constructor = type.GetConstructor(Array.Empty<Type>());
			if (constructor is not null)
			{
				if (constructor.Invoke(Array.Empty<object>()) is not TApi result)
					throw new InvalidOperationException(Resources.Backend.CannotInitialize);
				return result;
			}
			else
			{
				var constructors = type.GetConstructors();
				if (constructors.Length == 0 || !constructors.Contains(0, static c => c.GetParameters().Length))
					throw new InvalidOperationException(Resources.Backend.CannotInitialize);
				if (constructors.Where(c => c.GetParameters().Length == 0)[0].Invoke(Array.Empty<object>()) is not TApi result)
					throw new InvalidOperationException(Resources.Backend.CannotInitialize);
				return result;
			}
		}

		/// <summary>
		/// Initialize the given <paramref name="node"/> whose <see cref="LinkedListNode{TApi}.Value"/> is a <see cref="AbstractRuntimeApi{TApi}"/>
		/// </summary>
		/// <param name="node">The <see cref="LinkedListNode{TApi}"/> to initialize</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void Initialize(LinkedListNode<TApi> node)
		{
			if (node is null)
				throw new ArgumentNullException(nameof(node));
			if (!node.Value.Disposed)
			{
				return;
			}
			var type = node.Value.GetType();
			node.Value = Create(type);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void PromoteImplementation(TApi impl)
		{
			if (impl is null)
				return;
			if (!RecentAPIs.Contains(impl))
				return;
			RecentAPIs.Remove(impl);
			var node = RecentAPIs.AddFirst(impl);
			Initialize(node);
			if (Settings.DisposeNotCurrentImplementation)
			{
				node.Next?.Value?.Dispose();
			}
		}

		/// <summary>
		/// Set the current implementation in <see cref="RecentAPIs"/> (the first node) to a given <paramref name="implementation"/>
		/// </summary>
		/// <param name="implementation">The implementation indicated by a <see cref="Type"/></param>
		/// <returns>Success or not</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static bool SetImplementation(Type implementation)
		{
			if (implementation.IsGenericType || implementation.IsAbstract || !implementation.IsAssignableTo(typeof(TApi)))
			{
				return false;
			}
			// otherwise
			var current = RecentAPIs.First;
			while (current is not null)
			{
				if (current.Value.GetType() == implementation)
				{
					PromoteImplementation(current.Value);
					return true;
				}
				current = current.Next;
			}
			// a new implementation
			var node = RecentAPIs.AddFirst(Create(implementation));
			if (Settings.DisposeNotCurrentImplementation)
			{
				node.Next?.Value?.Dispose();
			}
			return true;
		}

		/// <summary>
		/// Set the current implementation in <see cref="RecentAPIs"/> (the first node) to a given <paramref name="implementation"/>
		/// </summary>
		/// <param name="implementation">The implementation indicated by a <typeparamref name="TApi"/></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static bool SetImplementation(TApi? implementation)
		{
			if (implementation is null)
				return false;
			if (RecentAPIs.Contains(implementation))
			{
				PromoteImplementation(implementation);
			}
			else
			{
				var node = RecentAPIs.AddFirst(implementation);
				if (Settings.DisposeNotCurrentImplementation)
				{
					node.Next?.Value?.Dispose();
				}
			}
			return true;
		}
		#endregion

		#region static methods used for dispatching
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void DisposeNotCurrent(LinkedList<TApi> RecentAPIs)
		{
			if (Settings.DisposeNotCurrentImplementation)
			{
				var node = RecentAPIs.First;
				while (node is not null)
				{
					node.Value.Dispose();
					node = node.Next;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void Check(LinkedList<TApi> RecentAPIs)
		{
			if (RecentAPIs is null)
				throw new ArgumentNullException(nameof(RecentAPIs));
			if (RecentAPIs.Count == 0)
				throw new InvalidOperationException(Resources.Backend.NotAvailable);
			DisposeNotCurrent(RecentAPIs);
		}

		/// <summary>
		/// Check the given recent API list <see cref="RecentAPIs"/> and the validness of given storage(s)
		/// </summary>
		/// <param name="storage1">The first storage to check validness</param>
		/// <exception cref="ArgumentNullException">If any of the given storage(s) is null or invalid</exception>
		/// <exception cref="InvalidOperationException">If <see cref="RecentAPIs"/> is empty (there is not available back-end)</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void Check(IStorage storage1)
		{
			if (storage1 is null || storage1.IsValid())
				throw new ArgumentNullException(nameof(storage1));
			Check(RecentAPIs);
		}

		/// <summary>
		/// Check the given recent API list <see cref="RecentAPIs"/> and the validness of given storage(s)
		/// </summary>
		/// <param name="storage1">The first storage to check validness</param>
		/// <param name="storage2">The second storage to check validness</param>
		/// <exception cref="ArgumentNullException">If any of the given storage(s) is null or invalid</exception>
		/// <exception cref="InvalidOperationException">If <see cref="RecentAPIs"/> is empty (there is not available back-end)</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void Check(IStorage storage1, IStorage storage2)
		{
			if (storage1 is null || storage1.IsValid())
				throw new ArgumentNullException(nameof(storage1));
			if (storage2 is null || storage2.IsValid())
				throw new ArgumentNullException(nameof(storage2));
			Check(RecentAPIs);
		}

		/// <summary>
		/// Check the given recent API list <see cref="RecentAPIs"/> and the validness of given storage(s)
		/// </summary>
		/// <param name="storage1">The first storage to check validness</param>
		/// <param name="storage2">The second storage to check validness</param>
		/// <param name="storage3">The third storage to check validness</param>
		/// <exception cref="ArgumentNullException">If any of the given storage(s) is null or invalid</exception>
		/// <exception cref="InvalidOperationException">If <see cref="RecentAPIs"/> is empty (there is not available back-end)</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void Check(IStorage storage1, IStorage storage2, IStorage storage3)
		{
			if (storage1 is null || storage1.IsValid())
				throw new ArgumentNullException(nameof(storage1));
			if (storage2 is null || storage2.IsValid())
				throw new ArgumentNullException(nameof(storage2));
			if (storage3 is null || storage3.IsValid())
				throw new ArgumentNullException(nameof(storage3));
			Check(RecentAPIs);
		}

		/// <summary>
		/// Check the given recent API list <see cref="RecentAPIs"/> and the validness of given storage(s)
		/// </summary>
		/// <param name="storage1">The first storage to check validness</param>
		/// <param name="storage2">The second storage to check validness</param>
		/// <param name="storage3">The third storage to check validness</param>
		/// <param name="storage4">The fourth storage to check validness</param>
		/// <exception cref="ArgumentNullException">If any of the given storage(s) is null or invalid</exception>
		/// <exception cref="InvalidOperationException">If <see cref="RecentAPIs"/> is empty (there is not available back-end)</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void Check(IStorage storage1, IStorage storage2, IStorage storage3, IStorage storage4)
		{
			if (storage1 is null || storage1.IsValid())
				throw new ArgumentNullException(nameof(storage1));
			if (storage2 is null || storage2.IsValid())
				throw new ArgumentNullException(nameof(storage2));
			if (storage3 is null || storage3.IsValid())
				throw new ArgumentNullException(nameof(storage3));
			if (storage4 is null || storage4.IsValid())
				throw new ArgumentNullException(nameof(storage4));
			Check(RecentAPIs);
		}

		/// <summary>
		/// Check the given recent API list <see cref="RecentAPIs"/> and the validness of given storages
		/// </summary>
		/// <param name="storages">The storages to check validness</param>
		/// <exception cref="ArgumentNullException">If any of the given <paramref name="storages"/> is null or invalid</exception>
		/// <exception cref="InvalidOperationException">If <see cref="RecentAPIs"/> is empty (there is not available back-end)</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void Check(IReadOnlyList<IStorage> storages)
		{
			if (storages is null || storages.Any(static s => s is null || !s.IsValid()))
				throw new ArgumentNullException(nameof(storages));
			Check(RecentAPIs);
		}

		/// <summary>
		/// Select the most recent implementation in <see cref="RecentAPIs"/> which fits the given predicate <paramref name="validApi"/>
		/// </summary>
		/// <param name="validApi">A <see cref="Predicate{TApi}"/> used to check other validness of the candidate implementations</param>
		/// <param name="from">The starting <see cref="LinkedListNode{TApi}"/> to begin searching, default null means search from the first of <see cref="RecentAPIs"/></param>
		/// <returns>The suitable most recent implementation as a <see cref="LinkedListNode{TApi}"/> or error if not found.</returns>
		/// <exception cref="InvalidOperationException">if there is no available back-end implementation or the suitable one cannot be initialized</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static LinkedListNode<TApi> SelectImplementation(Predicate<TApi> validApi, LinkedListNode<TApi>? from = null)
		{
			if (validApi is null)
				throw new ArgumentNullException(nameof(validApi));
			Check(RecentAPIs);

			var current = from?.Next ?? RecentAPIs.First;
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
				throw new ArgumentOutOfRangeException(nameof(N), N, Resources.Parameter.MustPositive);
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
		#endregion


		#region disposition and serialization
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
		/// When implemented by a derived class, actually the release all unmanaged resources held by this runtime API.
		/// </summary>
		/// <param name="disposeManaged"></param>
		protected abstract void Dispose(bool disposeManaged);

		/// <summary>
		/// When implemented by a derived class, get the <see cref="JsonConverter{T}"/> used to serialize it (deserialization is done via default of <see cref="JsonSerializer"/> or <see cref="JsonConstructorAttribute"/>). The default implementation simply returns null.
		/// </summary>
		protected internal virtual JsonConverter<AbstractRuntimeApi<TApi>>? CurrentConverter => null;
		#endregion


		#region dynamic method invocation
		#region structure
		/// <summary>
		/// The structure used to store the extra methods' information
		/// </summary>
		protected readonly struct ExtraMethodInfo : IEquatable<ExtraMethodInfo>, IReadOnlyList<Type>
		{
			private readonly FixedClassBuffer_16<Type> inputTypes;

			private readonly string name;

			private readonly int genericCount;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private static void CheckMethodInfo(string name, int genericParameterCount, ReadOnlySpan<Type> inputTypes)
			{
				if (name is null)
					throw new ArgumentNullException(nameof(name));
				if (inputTypes.IsEmpty)
					throw new ArgumentNullException(nameof(inputTypes));
				if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-zA-Z_]\w+$"))
					throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(name));
				if (genericParameterCount < 0)
					throw new ArgumentOutOfRangeException(nameof(genericParameterCount), genericParameterCount, Resources.Parameter.CannotNegative);
				if (inputTypes.Length > 16)
					throw new ArgumentException(Resources.Parameter.WrongSize, nameof(inputTypes));
			}

			/// <summary>
			/// Create an <see cref="ExtraMethodInfo"/> from given <paramref name="name"/> and <paramref name="inputTypes"/>
			/// </summary>
			/// <param name="name">The name of the method, must be a valid method name</param>
			/// <param name="genericParameterCount">The number of generic parameters</param>
			/// <param name="inputTypes">The input types as a <see cref="ReadOnlySpan{T}"/> of <see cref="Type"/>s</param>
			/// <exception cref="ArgumentNullException">If <paramref name="name"/> is null or <paramref name="inputTypes"/> is empty</exception>
			/// <exception cref="ArgumentException">If <paramref name="name"/> is not a valid method name, or <paramref name="inputTypes"/> is too large</exception>
			/// <exception cref="ArgumentOutOfRangeException">If <paramref name="genericParameterCount"/> is less than 0</exception>
			public ExtraMethodInfo(string name, int genericParameterCount, ReadOnlySpan<Type> inputTypes)
			{
				CheckMethodInfo(name, genericParameterCount, inputTypes);
				this.inputTypes = default;
				this.genericCount = genericParameterCount;
				this.name = name;
				for (int i = 0; i < inputTypes.Length; i++)
				{
					this.inputTypes[i] = inputTypes[i];
				}
			}

			#region list
			/// <summary>
			/// Get the method name as a <see cref="string"/>
			/// </summary>
			public string Name {
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => this.name;
			}

			/// <summary>
			/// Get the number of generic parameters of the method
			/// </summary>
			public int GenericParameterCount {
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => this.genericCount;
			}

			/// <summary>
			/// Get the number of input arguments of this <see cref="ExtraMethodInfo"/>
			/// </summary>
			public int Count {
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get {
					int find = this.inputTypes.AsSpan().IndexOf(static t => t.Equals(default));
					return find < 0 ? this.inputTypes.Count : find;
				}
			}

			/// <summary>
			/// Get the <paramref name="index"/>-th input argument's <see cref="RuntimeTypeHandle"/> of this <see cref="ExtraMethodInfo"/>
			/// </summary>
			/// <param name="index">The index of the input argument</param>
			/// <returns>The <paramref name="index"/>-th input argument's <see cref="RuntimeTypeHandle"/></returns>
			/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range</exception>
			public Type this[int index] {
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get {
					if (index < 0 || index >= this.inputTypes.Count)
						throw new ArgumentOutOfRangeException(nameof(index), index, Resources.Parameter.InvalidValue);
					return this.inputTypes[index];
				}
			}

			/// <summary>
			/// Get the enumerator of this <see cref="ExtraMethodInfo"/>
			/// </summary>
			/// <returns>The enumerator of this <see cref="ExtraMethodInfo"/></returns>
			public IEnumerator<Type> GetEnumerator()
			{
				for (int i = 0; i < this.inputTypes.Count; i++)
				{
					yield return this.inputTypes[i];
				}
			}

			IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
			#endregion

			/// <summary>
			/// Indicates whether the current object is equal to another object of the same type.
			/// </summary>
			/// <param name="other">The other <see cref="ExtraMethodInfo"/> to compare with this one.</param>
			/// <returns>True if the current object is equal to the other parameter; otherwise, false.</returns>
			public bool Equals(ExtraMethodInfo other) => this.name == other.name && this.genericCount == other.genericCount && this.inputTypes == other.inputTypes;

			/// <summary>
			/// Indicates whether the current object is equal to another object of the same type.
			/// </summary>
			/// <param name="obj">The other object to compare with this one.</param>
			/// <returns>True if the current object is equal to the other parameter; otherwise, false.</returns>
			public override bool Equals(object? obj) => obj is ExtraMethodInfo handle && this.Equals(handle);

			/// <summary>
			/// Get the hash code of this <see cref="ExtraMethodInfo"/>
			/// </summary>
			/// <returns>The hash code of this <see cref="ExtraMethodInfo"/></returns>
			public override int GetHashCode() => HashCode.Combine(this.name, this.genericCount, this.inputTypes);

			/// <summary>
			/// Equality operator
			/// </summary>
			public static bool operator ==(ExtraMethodInfo left, ExtraMethodInfo right) => left.Equals(right);

			/// <summary>
			/// Inequality operator
			/// </summary>
			public static bool operator !=(ExtraMethodInfo left, ExtraMethodInfo right) => !left.Equals(right);
		}
		#endregion

		#region register and invoke
		private static readonly Dictionary<ExtraMethodInfo, DynamicMethod> extraStaticMethods = new();

		private static readonly Dictionary<ExtraMethodInfo, LinkedList<(TApi api, MethodInfo method)>> extraMethodsInfo = new();

		/// <summary>
		/// Find the method with given <paramref name="name"/> and <paramref name="inputTypes"/> (optional) in all implementations of <typeparamref name="TApi"/>. Then register the method to prepare it for dynamic invocation.
		/// </summary>
		/// <param name="name">The name of the given method, must be a legal method name</param>
		/// <param name="returnType">The return value type or null if the method has no return.</param>
		/// <param name="genericCount">The number of generic parameters</param>
		/// <param name="inputTypes">The input types as an array of <see cref="Type"/>. If this is null or empty, <paramref name="name"/> alone must be enough to determine the given method.</param>
		/// <returns>Success or not.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is null or <paramref name="inputTypes"/> is empty</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is not a valid method name, or <paramref name="inputTypes"/> is too large</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="genericCount"/> is less than 0</exception>
		protected static bool RegisterExtraMethod(string name, int genericCount, Type? returnType, params Type[] inputTypes)
		{
			// change 'extraMethodsApi'
			bool found = false;
			var info = new ExtraMethodInfo(name, genericCount, inputTypes);
			var tempInputTypes = inputTypes;
			if (returnType is not null)
			{
				tempInputTypes = new Type[inputTypes.Length + 1];
				Array.Copy(inputTypes, tempInputTypes, inputTypes.Length);
				tempInputTypes[^1] = returnType.MakeByRefType(); // make 'returnType' an output type
			}
			const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
			foreach (var item in RecentAPIs)
			{
				Type t = item.GetType();
				var methodInfo = t.GetMethod(name, genericCount, bindingFlags, null, tempInputTypes, null);
				if (methodInfo is null)
				{
					if (name.EndsWith('_'))
						methodInfo = t.GetMethod(name[..^1], genericCount, bindingFlags, null, tempInputTypes, null);
					else
						methodInfo = t.GetMethod(name + "_", genericCount, bindingFlags, null, tempInputTypes, null);
				}
				if (methodInfo is null || methodInfo.ReturnType != typeof(bool))
					continue;
				// add
				if (extraMethodsInfo.TryGetValue(info, out var ll))
				{
					LinkedListNode<(TApi api, MethodInfo method)>? node = ll.First;
					while (node is not null)
					{
						if (node.Value.api == item)
						{
							node.Value = (item, methodInfo);
							break;
						}
						node = node.Next;
					}
					if (node is null)
						ll.AddLast((item, methodInfo));
				}
				else
				{
					var tempList = new LinkedList<(TApi, MethodInfo)>();
					tempList.AddLast((item, methodInfo));
					extraMethodsInfo.Add(info, tempList);
				}
				found = true;
			}
			if (!found)
				return false;

			// create dynamic method
			var method = new DynamicMethod(name, returnType, inputTypes, typeof(AbstractRuntimeApi<TApi>), false);
			var IL = method.GetILGenerator();
			IL.DeclareLocal(typeof(bool)); //// bool success = false;
			if (returnType is not null)
				IL.DeclareLocal(returnType); //// returnType result = default;
			foreach (var (_, methodInfo) in extraMethodsInfo[info])
			{
				
				IL.Emit(OpCodes.Call, methodInfo);
			}
			// add
			extraStaticMethods.Add(info, method);
			return true;
		}
		#endregion
		#endregion
	}
}
