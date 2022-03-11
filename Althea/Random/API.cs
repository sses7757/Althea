using System;
using System.Collections.Generic;
using System.Dynamic;

using Althea.NativeTypes;
using Althea.Linq;


namespace Althea.Random
{
	/// <summary>
	/// The abstract class for runtime random API routines 
	/// </summary>
	public abstract partial class AbstractApi : AbstractApiSelector
	{
		#region basic
		/// <summary>
		/// Get the currently using <see cref="AbstractApi"/>.
		/// </summary>
		/// <remarks><b>DO NOT</b> invoke methods of this property directly unless you are sure about what you are doing; otherwise, there may be exceptions and / or unnoticeable bugs.</remarks>
		public static AbstractApi? Current => RecentAPIs.First?.Value;

		private static readonly LinkedList<AbstractApi> RecentAPIs = new();

		/// <summary>
		/// Set the currently using <see cref="AbstractApi"/> to the given <paramref name="implementation"/>
		/// </summary>
		/// <param name="implementation">The <see cref="Type"/> of the given implementation of <see cref="AbstractApi"/></param>
		/// <returns>Success or not.</returns>
		public static bool SetImplementation(Type implementation) => SetImplementation(RecentAPIs, implementation);

		/// <summary>
		/// Set the currently using <see cref="AbstractApi"/> to the given <paramref name="implementation"/>
		/// </summary>
		/// <param name="implementation">The instance of an implementation of <see cref="AbstractApi"/></param>
		/// <returns>Success or not.</returns>
		internal static bool SetImplementation(AbstractApi? implementation) => SetImplementation(RecentAPIs, implementation);
		#endregion


		#region dynamic invocation
		/// <summary>
		/// Get the dynamic object used to dynamically invoke method(s) not listed explicitly here (the methods extra defined in derived classes)
		/// </summary>
		/// <remarks>
		/// Due to the limitations of dynamic invocation, <c>ref</c>, <c>in</c>, <c>out</c> and <c>ref struct</c>, etc. are not supported and non of the input arguments can be null.<br/>
		/// Since there are internal caching for <see cref="DynamicObject.TryInvokeMember(InvokeMemberBinder, object[], out object)"/>, the average repeated dynamic invocation may cost around 1 microsecond.
		/// </remarks>
		/// <example><code>
		/// long number = AbstractApi.Dynamic.CholeskyDecompose(...);
		/// </code></example>
		public static dynamic Dynamic => singletonDynamic;

		private static readonly DynamicInvocations singletonDynamic = new();

		private sealed class DynamicInvocations : DynamicInvocation
		{
			public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
			{
				result = DynamicInvokeExtraMethod(RecentAPIs, binder.Name, args);
				return true;
			}
		}
		#endregion


		#region support information
		/// <summary>
		/// When implemented by a derived class, check if the given <see cref="CombinationOfLocations"/> is supported by unary operations of this implementation or not.
		/// </summary>
		/// <param name="location1">The first given <see cref="CombinationOfLocations"/></param>
		/// <returns>Whether unary operations on <paramref name="location1"/> is supported by this <see cref="AbstractApi"/>.</returns>
		protected abstract bool IsSupportedUnary(CombinationOfLocations location1);

		/// <summary>
		/// When implemented by a derived class, check if the given <see cref="CombinationOfLocations"/>s are supported by binary operations of this implementation or not.
		/// </summary>
		/// <param name="location1">The first given <see cref="CombinationOfLocations"/></param>
		/// <param name="location2">The second given <see cref="CombinationOfLocations"/></param>
		/// <returns>Whether binary operations on <paramref name="location1"/> and <paramref name="location2"/> are supported by this <see cref="AbstractApi"/>.</returns>
		protected abstract bool IsSupportedBinary(CombinationOfLocations location1, CombinationOfLocations location2);

		/// <summary>
		/// When implemented by a derived class, check if the given <see cref="CombinationOfLocations"/>s are supported by trinary operations of this implementation or not.
		/// </summary>
		/// <param name="location1">The first given <see cref="CombinationOfLocations"/></param>
		/// <param name="location2">The second given <see cref="CombinationOfLocations"/></param>
		/// <param name="location3">The third given <see cref="CombinationOfLocations"/></param>
		/// <returns>Whether trinary operations on <paramref name="location1"/>, <paramref name="location2"/> and <paramref name="location3"/> are supported by this <see cref="AbstractApi"/>.</returns>
		protected abstract bool IsSupportedTrinary(CombinationOfLocations location1, CombinationOfLocations location2, CombinationOfLocations location3);

		// Ignore Spelling: N-ary
		/// <summary>
		/// When implemented by a derived class, check if the given <see cref="CombinationOfLocations"/>s are supported by N-ary operations of this implementation or not.
		/// </summary>
		/// <param name="locations">The given <see cref="CombinationOfLocations"/>s</param>
		/// <returns>Whether N-ary operations on <paramref name="locations"/> are supported by this <see cref="AbstractApi"/>.</returns>
		protected abstract bool IsSupportedNary(ReadOnlySpan<CombinationOfLocations> locations);

		/// <summary>
		/// Get the default one-dimensional <see cref="IRandomDistribution"/> -- a <see cref="UniformDistribution{T}"/> on [0, 1) if <typeparamref name="T"/> is a floating-point type, otherwise a <see cref="RandomBitsDistribution{T}"/>.
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <returns>The default one-dimensional <see cref="IRandomDistribution"/></returns>
		public static IRandomDistribution GetDefaultDistribution<T>() where T : unmanaged, INumber<T>
			=> Const<T>.IsIntegralType ? new RandomBitsDistribution<T>() : new UniformDistribution<T>(Const<T>.One);
		#endregion


		#region static methods as dispatchers
		/// <summary>
		/// Fill the given <paramref name="storage"/> with random numbers generated from the given <paramref name="distribution"/>
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="storage">The <see cref="Storage{T}"/> to be filled with random numbers</param>
		/// <param name="distribution">The <see cref="IRandomDistribution"/> indicating which distribution to use, must be of rank-1. Default null means <see cref="GetDefaultDistribution{T}"/>.</param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="storage"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="distribution"/> is not of rank-1 or its data type is not <typeparamref name="T"/></exception>
		public static void FillWithRandom<T>(Storage<T> storage, IRandomDistribution? distribution = null) where T : unmanaged, INumber<T>
		{
			distribution ??= GetDefaultDistribution<T>();
			if (distribution[0] != Const<T>.DataType)
				throw new ArgumentException(Resources.Parameter.UnexpectedType, nameof(distribution));

			CombinationOfLocations location1 = storage.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedUnary(location1), node);
				success = node.Value.FillWithRandom_(storage, distribution);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Fill the given <paramref name="storage1"/> and <paramref name="storage2"/> with random numbers generated from the given <paramref name="distribution"/>
		/// </summary>
		/// <typeparam name="T1">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="T2">Any unmanaged number as the data type</typeparam>
		/// <param name="storage1">The first <see cref="Storage{T}"/> to be filled with random numbers</param>
		/// <param name="storage2">The second <see cref="Storage{T}"/> to be filled with random numbers</param>
		/// <param name="distribution">The <see cref="IRandomDistribution"/> indicating which distribution to use, must be of rank-2</param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="storage1"/> or <paramref name="storage2"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="distribution"/> is not of rank-2 or its data types are not <typeparamref name="T1"/> and <typeparamref name="T2"/></exception>
		public static void FillWithRandom<T1, T2>(Storage<T1> storage1, Storage<T2> storage2, IRandomDistribution distribution)
			where T1 : unmanaged
			where T2 : unmanaged
		{
			if (distribution is null )
				throw new ArgumentNullException(nameof(distribution));
			if (distribution.Count != 2)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(distribution));
			if (distribution[0] != Const<T1>.DataType || distribution[1] != Const<T2>.DataType)
				throw new ArgumentException(Resources.Parameter.UnexpectedType, nameof(distribution));

			CombinationOfLocations location1 = storage1.LocationDescription, locatio2 = storage2.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedBinary(location1, locatio2), node);
				success = node.Value.FillWithRandom_(storage1, storage2, distribution);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Fill the given <paramref name="storage1"/>, <paramref name="storage2"/> and <paramref name="storage3"/> with random numbers generated from the given <paramref name="distribution"/>
		/// </summary>
		/// <typeparam name="T1">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="T2">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="T3">Any unmanaged number as the data type</typeparam>
		/// <param name="storage1">The first <see cref="Storage{T}"/> to be filled with random numbers</param>
		/// <param name="storage2">The second <see cref="Storage{T}"/> to be filled with random numbers</param>
		/// <param name="storage3">The third <see cref="Storage{T}"/> to be filled with random numbers</param>
		/// <param name="distribution">The <see cref="IRandomDistribution"/> indicating which distribution to use, must be of rank-3</param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="storage1"/> or <paramref name="storage2"/> or <paramref name="storage3"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="distribution"/> is not of rank-3 or its data types are not <typeparamref name="T1"/>, <typeparamref name="T2"/> and <typeparamref name="T3"/></exception>
		public static void FillWithRandom<T1, T2, T3>(Storage<T1> storage1, Storage<T2> storage2, Storage<T3> storage3, IRandomDistribution distribution)
			where T1 : unmanaged
			where T2 : unmanaged
			where T3 : unmanaged
		{
			if (distribution is null)
				throw new ArgumentNullException(nameof(distribution));
			if (distribution.Count != 3)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(distribution));
			if (distribution[0] != Const<T1>.DataType || distribution[1] != Const<T2>.DataType || distribution[2] != Const<T3>.DataType)
				throw new ArgumentException(Resources.Parameter.UnexpectedType, nameof(distribution));

			CombinationOfLocations location1 = storage1.LocationDescription, locatio2 = storage2.LocationDescription, locatio3 = storage3.LocationDescription;
			bool success = false;
			LinkedListNode<AbstractApi>? node = null;
			while (!success)
			{
				node = SelectImplementation(a => a.IsSupportedTrinary(location1, locatio2, locatio3), node);
				success = node.Value.FillWithRandom_(storage1, storage2, storage3, distribution);
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
		}

		/// <summary>
		/// Fill the given <paramref name="storages"/> with random numbers generated from the given <paramref name="distribution"/>
		/// </summary>
		/// <param name="distribution">The <see cref="IRandomDistribution"/> indicating which distribution to use, must be of rank equaling the length of <paramref name="storages"/></param>
		/// <param name="storages">The array of <see cref="IStorage"/>s to be filled with random numbers</param>
		/// <exception cref="InvalidOperationException">If an error occurred during selecting the implementation</exception>
		/// <exception cref="ArgumentNullException">If any of <paramref name="storages"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="distribution"/> is of wrong rank or its data types are not the data types of <paramref name="storages"/></exception>
		public static void FillWithRandom(IRandomDistribution distribution, params IStorage[] storages)
		{
			if (distribution is null)
				throw new ArgumentNullException(nameof(distribution));
			if (storages is null || storages.Length == 0)
				throw new ArgumentNullException(nameof(storages));
			if (distribution.Count != storages.Length)
				throw new ArgumentException(Resources.Parameter.NotSameSize);
			if (!distribution.SequenceEqual(storages, static (d, s) => d == s.DataType))
				throw new ArgumentException(Resources.Parameter.UnexpectedType);

			Span<CombinationOfLocations> locations = stackalloc CombinationOfLocations[storages.Length];
			storages.CopyTo(locations, static s => s.LocationDescription);
			bool success = false;
			LinkedListNode<AbstractApi>? node = RecentAPIs.First;
			while (!success)
			{
				while (node is not null)
				{
					Initialize(node);
					if (node.Value.IsSupportedNary(locations))
					{
						success = node.Value.FillWithRandom_(distribution, storages);
						if (success)
							break;
					}
					node = node.Next;
				}
			}
			if (success && node is not null)
				SetImplementation(RecentAPIs, node.Value);
			else
				throw new InvalidOperationException(Resources.Backend.NotAvailable);
		}
		#endregion


		#region abstract methods that actually do computations
		/// <summary>
		/// When implemented by a derived class, fill the given <paramref name="storage"/> with random numbers generated from the given <paramref name="distribution"/>
		/// </summary>
		/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
		/// <param name="storage">The <see cref="Storage{T}"/> to be filled with random numbers</param>
		/// <param name="distribution">The <see cref="IRandomDistribution"/> indicating which distribution to use, must be of rank-1</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="storage"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="distribution"/> is not of rank-1 or its data type is not <typeparamref name="T"/></exception>
		protected abstract bool FillWithRandom<T>(Storage<T> storage, IRandomDistribution distribution) where T : unmanaged, INumber<T>;

		/// <summary>
		/// When implemented by a derived class, fill the given <paramref name="storage1"/> and <paramref name="storage2"/> with random numbers generated from the given <paramref name="distribution"/>
		/// </summary>
		/// <typeparam name="T1">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="T2">Any unmanaged number as the data type</typeparam>
		/// <param name="storage1">The first <see cref="Storage{T}"/> to be filled with random numbers</param>
		/// <param name="storage2">The second <see cref="Storage{T}"/> to be filled with random numbers</param>
		/// <param name="distribution">The <see cref="IRandomDistribution"/> indicating which distribution to use, must be of rank-2</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="storage1"/> or <paramref name="storage2"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="distribution"/> is not of rank-2 or its data types are not <typeparamref name="T1"/> and <typeparamref name="T2"/></exception>
		protected abstract bool FillWithRandom<T1, T2>(Storage<T1> storage1, Storage<T2> storage2, IRandomDistribution distribution)
			where T1 : unmanaged
			where T2 : unmanaged;

		/// <summary>
		/// When implemented by a derived class, fill the given <paramref name="storage1"/>, <paramref name="storage2"/> and <paramref name="storage3"/> with random numbers generated from the given <paramref name="distribution"/>
		/// </summary>
		/// <typeparam name="T1">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="T2">Any unmanaged number as the data type</typeparam>
		/// <typeparam name="T3">Any unmanaged number as the data type</typeparam>
		/// <param name="storage1">The first <see cref="Storage{T}"/> to be filled with random numbers</param>
		/// <param name="storage2">The second <see cref="Storage{T}"/> to be filled with random numbers</param>
		/// <param name="storage3">The third <see cref="Storage{T}"/> to be filled with random numbers</param>
		/// <param name="distribution">The <see cref="IRandomDistribution"/> indicating which distribution to use, must be of rank-3</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="storage1"/> or <paramref name="storage2"/> or <paramref name="storage3"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="distribution"/> is not of rank-3 or its data types are not <typeparamref name="T1"/>, <typeparamref name="T2"/> and <typeparamref name="T3"/></exception>
		protected abstract bool FillWithRandom<T1, T2, T3>(Storage<T1> storage1, Storage<T2> storage2, Storage<T3> storage3, IRandomDistribution distribution)
			where T1 : unmanaged
			where T2 : unmanaged
			where T3 : unmanaged;

		/// <summary>
		/// When implemented by a derived class, fill the given <paramref name="storages"/> with random numbers generated from the given <paramref name="distribution"/>
		/// </summary>
		/// <param name="distribution">The <see cref="IRandomDistribution"/> indicating which distribution to use, must be of rank equaling the length of <paramref name="storages"/></param>
		/// <param name="storages">The array of <see cref="IStorage"/>s to be filled with random numbers</param>
		/// <returns>Whether this implementation supports the given parameters or not. If false, further internal operation is not allowed.</returns>
		/// <exception cref="ArgumentNullException">If any of <paramref name="storages"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="distribution"/> is of wrong rank or its data types are not the data types of <paramref name="storages"/></exception>
		protected abstract bool FillWithRandom_(IRandomDistribution distribution, params IStorage[] storages);
		#endregion
	}
}