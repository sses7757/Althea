using System.Runtime.CompilerServices;
using System.Text.Json;

using Althea.Helpers;
using Althea.Linq;
using Althea.Storage;


namespace Althea.Array
{
	#region manager
	/// <summary>
	/// The static thread-safe manager for managing cross referenced <see cref="IStorage"/>s of arrays.
	/// </summary>
	public static class ArrayStorageManager
	{
		private sealed class RefCount
		{
			public int count = 1;
		}
		private static readonly ConditionalWeakTable<object, RefCount> ReferenceCountings = new();

		/// <summary>
		/// Add the given <paramref name="storage"/> to the manager whose <see cref="IStorage.Reference"/> is used to determine whether it is a referenced storage or not.
		/// </summary>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage"/></typeparam>
		/// <param name="storage">The storage to be managed</param>
		/// <returns>Returns the <paramref name="storage"/> itself.</returns>
		public static TS AddToManager<TS>(this TS storage) where TS : class, IStorage
		{
			if (!storage.IsValid())
				return storage;
			////lock (ReferenceCountings)
			{
				var storageRoot = storage.Reference ?? storage;
				if (ReferenceCountings.TryGetValue(storageRoot, out var refCount))
					refCount.count++;
				else
					ReferenceCountings.Add(storageRoot, new());
			}
			return storage;
		}

		/// <summary>
		/// Safely dispose the given <paramref name="storage"/> by checking references before invoking <see cref="IDisposable.Dispose"/>.
		/// </summary>
		/// <typeparam name="TS">The actual storage type that implements <see cref="IStorage"/></typeparam>
		/// <param name="storage">The storage to be disposed</param>
		/// <param name="invokedByUser">Whether this method is invoked by user or by GC</param>
		public static void SafeDispose<TS>(this TS? storage, bool invokedByUser = true) where TS : class, IStorage
		{
			if (storage is null || storage.Disposed)
				return;
			////lock (ReferenceCountings)
			{
				var storageRoot = storage.Reference ?? storage;
				if (ReferenceCountings.TryGetValue(storageRoot, out var refCount))
				{
					refCount.count--;
					if (refCount.count == 0)
					{
						ReferenceCountings.Remove(storageRoot);
						storageRoot.Dispose(invokedByUser);
					}
				}
				else
				{
					storage.Dispose(invokedByUser);
				}
			}
		}
	}
	#endregion

	/// <summary>
	/// The abstract interface for value arrays.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TSelf">The concrete type that implements this <see cref="IValueArray{T, TSelf}"/></typeparam>
	/// <remarks>
	/// All inherited classes shall be of column major if not specified.<br/>
	/// All inherited classes shall use <see cref="ArrayStorageManager.AddToManager{TS}"/> in constructors and <see cref="ArrayStorageManager.SafeDispose{TS}"/> in <see cref="IDisposable.Dispose"/> rather than fininalizers to improve GC performance.
	/// </remarks>
	public interface IValueArray<T, TSelf> : ICheckValid, IDisposable, IPrintable<T>,
		ICreateAlike<TSelf>, IMainPropertyFormattable<TSelf>, System.Numerics.IEqualityOperators<TSelf, TSelf, bool>
		where T : unmanaged, IBaseNumber<T>
		where TSelf : class, IValueArray<T, TSelf>
	{
		#region basic
		/// <summary>
		/// When implemented by a derived class, get the size (in <typeparamref name="T"/>) of this array (the extent at each dimension) as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>.
		/// </summary>
		protected ReadOnlySpan<long> Size { get; }

		/// <summary>
		/// When implemented by a derived class, get the presenting length (in <typeparamref name="T"/>) of this array.
		/// </summary>
		protected long Length { get; }

		/// <summary>
		/// When implemented by a derived class, statically get an empty array of type <typeparamref name="TSelf"/>.
		/// </summary>
		public abstract static TSelf Empty { get; }

		/// <summary>
		/// When implemented by a derived class, copy this array's elements to <paramref name="destination"/>'s ones.
		/// </summary>
		/// <param name="destination">The destination array to copy to</param>
		/// <exception cref="ArgumentException">If <paramref name="destination"/> is not of same size as this one</exception>
		void CopyTo(TSelf destination);

		TSelf ICloneable<TSelf>.Clone()
		{
			TSelf clone = this.CreateAlike();
			try
			{
				this.CopyTo(clone);
				return clone;
			}
			catch (Exception)
			{
				clone?.Dispose();
				throw;
			}
		}
		#endregion

		#region point-wise operations
		/// <summary>
		/// When implemented by a derived class, fill this array with given <paramref name="value"/>.
		/// </summary>
		/// <param name="value">The value as a <typeparamref name="T"/> to fill</param>
		void FillWith(T value);

		/// <summary>
		/// When implemented by a derived class, point-wisely in-place add this array with given <paramref name="value"/>.
		/// </summary>
		/// <param name="value">The scalar as <typeparamref name="T"/> to add</param>
		void AddScalar(T value);

		/// <summary>
		/// When implemented by a derived class, point-wisely in-place multiply this array with given <paramref name="value"/>.
		/// </summary>
		/// <param name="value">The scalar as <typeparamref name="T"/> to multiply</param>
		void Scale(T value);

		/// <summary>
		/// When implemented by a derived class, point-wisely in-place conjugate this array.
		/// </summary>
		void Conjugate();
		#endregion

		#region simple aggregation operations
		/// <summary>
		/// When implemented by a derived class, aggregately sum the elements in this array.
		/// </summary>
		/// <returns>The aggregate sum of this array.</returns>
		T Sum();

		/// <summary>
		/// When implemented by a derived class, aggregately sum the absolute values of elements in this array.
		/// </summary>
		/// <returns>The aggregate sum of absolute values of this array.</returns>
		T AbsSum();

		/// <summary>
		/// When implemented by a derived class, compute the 2-norm (Euclidean norm) of elements in this array as if this is a vector.
		/// </summary>
		/// <returns>The 2-norm of this array.</returns>
		T Norm();

		/// <summary>
		/// When implemented by a derived class, in-place scale this array such that its 2-norm (Euclidean norm) is 1. The default implementation utilizes the <see cref="Norm()"/> and <see cref="Scale(T)"/>.
		/// </summary>
		virtual void Normalize() => this.Scale(T.One / this.Norm());

		/// <summary>
		/// When implemented by a derived class, get the element whose absolute value is maximum in this array.
		/// </summary>
		/// <returns>The element whose absolute value is maximum in this array.</returns>
		T ValueWithMaxAbs();

		/// <summary>
		/// When implemented by a derived class, get the element whose absolute value is minimum in this array.
		/// </summary>
		/// <returns>The element whose absolute value is minimum in this array.</returns>
		T ValueWithMinAbs();

		/// <summary>
		/// Compare this array with a given <paramref name="value"/> to check whether all elements are the same as <paramref name="value"/>.
		/// </summary>
		/// <param name="value">The given value in <typeparamref name="T"/> to compare</param>
		/// <returns>True if all elements in this array is the same as <paramref name="value"/>; false otherwise.</returns>
		public virtual bool ValueAllEquals(T value)
		{
			if (!this.IsValid())
				return false;
			T max = this.ValueWithMaxAbs();
			if (value == T.Zero)
				return max == T.Zero;
			T min = this.ValueWithMinAbs();
			return max == value && min == value;
		}
		#endregion

		#region reshape check
		/// <summary>
		/// Check the new size (dimensionality) to reshape to with respect to the original <paramref name="array"/> and find out the uncertain dimension.
		/// </summary>
		/// <param name="array">The original array as a <typeparamref name="TSelf"/> to check</param>
		/// <param name="newSize">The new size as a <see cref="Span{T}"/> to check which can have at most one uncertain dimension indicated by a non-positive number. Overwritten by the new size without uncertain dimension at exit.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="newSize"/> is of length 0</exception>
		/// <exception cref="ArgumentException">If <paramref name="newSize"/> is of length 2 and are all non-positive while the length of <paramref name="array"/> is not a perfect square; or <paramref name="newSize"/> has more than one uncertain dimensions</exception>
		/// <exception cref="ArgumentOutOfRangeException">If the product of <paramref name="newSize"/> is not the same as the presenting length of <paramref name="array"/></exception>
		protected static void CheckSize(TSelf array, Span<long> newSize)
		{
			if (newSize.Length == 0)
				throw new ArgumentNullException(nameof(newSize));
			// shortcut
			if (newSize.SequenceEqual(array.Size))
				return;

			if (newSize.Length == 2 && newSize[0] <= 0 && newSize[1] <= 0)
			{	// try to convert to a square matrix
				if (!array.Length.IsPerfectSquare())
				{
					throw new ArgumentException(Resources.ArithmeticError.PerfectSquare, nameof(array));
				}
				var leadDim = Convert.ToInt64(Math.Sqrt(array.Length));
				newSize[0] = newSize[1] = leadDim;
			}
			int firstFind = newSize.IndexOf(static r => r <= 0);
			if (firstFind < 0)
			{	// no uncertain index
				if (newSize.Prod() != array.Length)
					throw new ArgumentOutOfRangeException(nameof(newSize), newSize.Prod(), Resources.ParameterError.InvalidValue);
				return;
			}
			int lastFind = newSize.LastIndexOf(static r => r <= 0);
			if (lastFind == firstFind)
			{	// only one uncertainty
				newSize[firstFind] = 1;
				var prod = newSize.Prod();
				var remain = array.Length % prod;
				if (remain != 0)
					throw new ArgumentOutOfRangeException(nameof(newSize), remain, Resources.ParameterError.InvalidValue);
				else
					newSize[firstFind] = array.Length / prod;
			}
			else
			{	// more than one uncertain indices
				throw new ArgumentException(Resources.ParameterError.UnexpectedValue, nameof(newSize));
			}
		}
		#endregion

		#region serialization
		/// <summary>
		/// When implemented by a derived class, statically get the <see cref="JsonSerializerOptions"/> used to JSON serialize and deserialize <typeparamref name="TSelf"/>s whose <see cref="JsonSerializerOptions.Converters"/> shall contains necessary <see cref="IStorage{T, TSelf}.JsonConverter"/>(s).
		/// </summary>
		protected abstract static JsonSerializerOptions JsonSerializeOptions { get; }

		/// <summary>
		/// When implemented by a derived class, serialize this array to a JSON object. The default implementation simply utilizes <see cref="JsonSerializer.Serialize{TValue}(TValue, JsonSerializerOptions?)"/> and <see cref="JsonSerializeOptions"/>.
		/// </summary>
		/// <returns>The serialization of this array as a JSON object <see cref="string"/>.</returns>
		public virtual string JsonSerialize() => JsonSerializer.Serialize((object)this, TSelf.JsonSerializeOptions);
		
		/// <summary>
		/// Statically reconstruct a <typeparamref name="TSelf"/> from the given <paramref name="json"/> object <see cref="string"/>.
		/// </summary>
		/// <param name="json">The JSON object string used to deserialize</param>
		/// <returns>The reconstructed <typeparamref name="TSelf"/> from <paramref name="json"/>.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="json"/> null</exception>
		/// <exception cref="JsonException"></exception>
		/// <exception cref="NotSupportedException"></exception>
		public static TSelf JsonDeserialize(string json) => JsonSerializer.Deserialize<TSelf>(json, TSelf.JsonSerializeOptions) ?? throw new NotSupportedException();
		#endregion
	}
}
