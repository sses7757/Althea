using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;


namespace Althea.Helpers;

internal interface ICacher<TKey, TValue> : ICheckValid, IDisposable
	where TKey : notnull, IEquatable<TKey>
	where TValue : notnull, IDisposable
{
	protected static void Dispose<TOther>(Dictionary<TKey, (TValue value, TOther other)> dict)
	{
		if (dict is null)
			return;
		lock (dict)
		{
			foreach (var kv in dict)
			{
				kv.Value.value.Dispose();
			}
			dict.Clear();
		}
	}

	/// <summary>
	/// Try get the corresponding <paramref name="value"/> of the given <paramref name="key"/>.
	/// </summary>
	/// <remarks>This method implicitly updates the last visit time / hit count / etc. of <paramref name="key"/> if succeed.</remarks>
	/// <param name="key">The key used to get the value</param>
	/// <param name="value">The returned value if <paramref name="key"/> is present</param>
	/// <returns>Success or not.</returns>
	bool TryGetValue(in TKey key, [MaybeNullWhen(false)] out TValue value);
}


/// <summary>
/// The structure for thread-safe limited-sized cacher based on LRU algorithm.
/// </summary>
/// <remarks>The key-value pairs can only be added but not modified.<br/>
/// If the values are not read-only, the invokers shall be responsible for synchronizing them correctly.</remarks>
/// <typeparam name="TKey">The type of keys</typeparam>
/// <typeparam name="TValue">The type of values that must implements <see cref="IDisposable"/></typeparam>
public struct LimitSizedCacher<TKey, TValue> : ICacher<TKey, TValue>
	where TKey : notnull, IEquatable<TKey>
	where TValue : notnull, IDisposable
{
	private readonly Dictionary<TKey, (TValue value, DateTime lastVisit)> cached;

	private int capacity;

	/// <summary>
	/// Get or set the cache size of this <see cref="LimitSizedCacher{TKey, TValue}"/>
	/// </summary>
	public int Capacity
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		readonly get => this.capacity;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set
		{
			if (value < this.capacity)
				throw new ArgumentOutOfRangeException(nameof(value), value, Resources.ParameterError.UnexpectedValue);
			this.cached.EnsureCapacity(value);
			this.capacity = value;
		}
	}

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool IsValid() => this.capacity > 0;

	/// <summary>
	/// Create a new <see cref="LimitSizedCacher{TKey, TValue}"/> with given initial cache size
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public LimitSizedCacher(int cacheSize)
	{
		if (cacheSize <= 0)
			throw new ArgumentOutOfRangeException(nameof(cacheSize), Resources.ParameterError.MustPositive);
		this.capacity = cacheSize;
		this.cached = new(cacheSize);
	}

	/// <inheritdoc/>
	public readonly void Dispose() => ICacher<TKey, TValue>.Dispose(this.cached);

	/// <summary>
	/// Add to the cacher if <paramref name="key"/> is not present
	/// </summary>
	/// <param name="key">The key to add</param>
	/// <param name="value">The value to add</param>
	/// <returns>Success or not.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Add(in TKey key, TValue value)
	{
		lock (this.cached)
		{
			if (this.cached.ContainsKey(key))
				return false;
			if (this.cached.Count == this.capacity)
			{
				DateTime oldest = DateTime.MaxValue;
				TKey oldestKey = default!;
				foreach (var kv in this.cached)
				{
					if (kv.Value.lastVisit < oldest)
					{
						oldest = kv.Value.lastVisit;
						oldestKey = kv.Key;
					}
				}
				if (!this.cached.Remove(oldestKey))
					return false;
			}
			this.cached.Add(key, (value, DateTime.UtcNow));
			return true;
		}
	}

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool TryGetValue(in TKey key, [MaybeNullWhen(false)] out TValue value)
	{
		lock (this.cached)
		{
			value = default;
			if (!this.cached.TryGetValue(key, out var vl))
				return false;
			value = vl.value;
			vl.lastVisit = DateTime.UtcNow;
			this.cached[key] = vl;
			return true;
		}
	}

	/// <summary>
	/// Get the value of corresponding <paramref name="key"/>.
	/// </summary>
	/// <param name="key">The key used to get the value</param>
	/// <exception cref="KeyNotFoundException">If <paramref name="key"/> is not present in the underlying dictionary</exception>
	public readonly TValue this[in TKey key]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			if (!this.TryGetValue(key, out var value))
				throw new KeyNotFoundException();
			return value;
		}
	}
}


/// <summary>
/// The structure for thread-safe limited-sized cacher with candidates based on LRU algorithm.
/// </summary>
/// <remarks>The key-value pairs can only be added but not modified.<br/>
/// The <br/>
/// If the values are not read-only, the invokers shall be responsible for synchronizing them correctly.</remarks>
/// <typeparam name="TKey">The type of keys</typeparam>
/// <typeparam name="TValue">The type of values that must implements <see cref="IDisposable"/></typeparam>
public struct CandidateCacher<TKey, TValue> : ICacher<TKey, TValue>
	where TKey : notnull, IEquatable<TKey>
	where TValue : notnull, IDisposable
{
	private int capacity;

	private LimitSizedCacher<TKey, TValue> cached;

	private readonly Dictionary<TKey, int> candidates;

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool IsValid() => this.capacity > 0;

	/// <summary>
	/// Get or set the cache capacity of this <see cref="CandidateCacher{TKey, TValue}"/>
	/// </summary>
	public int CacheCapacity
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		readonly get => this.capacity;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => this.cached.Capacity = value;
	}

	/// <summary>
	/// Get or set the candidate capacity of this <see cref="CandidateCacher{TKey, TValue}"/>
	/// </summary>
	public int CandidateCapacity
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		readonly get => this.capacity;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set
		{
			if (value < this.capacity)
				throw new ArgumentOutOfRangeException(nameof(value), value, Resources.ParameterError.UnexpectedValue);
			this.candidates.EnsureCapacity(value);
			this.capacity = value;
		}
	}

	/// <summary>
	/// Get the threshold for candidates' hit counts -- if a candidate's hit count ≥ <see cref="HitCountThreshold"/>, then it will be added to the cached ones.
	/// </summary>
	public int HitCountThreshold { get; }

	private readonly ValueFactory factory;

	/// <summary>
	/// The delegate which will be invoked to generate a <typeparamref name="TValue"/> from given  <typeparamref name="TKey"/>.
	/// </summary>
	/// <param name="key">The key used to generate <typeparamref name="TValue"/></param>
	/// <param name="value">The generated <typeparamref name="TValue"/>.</param>
	public delegate bool ValueFactory(in TKey key, [MaybeNullWhen(false)] out TValue value);

	/// <summary>
	/// Create a new <see cref="CandidateCacher{TKey, TValue}"/> with given initial <paramref name="cacheSize"/> and <paramref name="candidateSize"/> and constant <paramref name="hitCountThreshold"/>.
	/// </summary>
	/// <exception cref="ArgumentException">If <paramref name="candidateSize"/> &lt; 2 * <paramref name="cacheSize"/></exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CandidateCacher(int cacheSize, int candidateSize, int hitCountThreshold, ValueFactory factory)
	{
		this.cached = new(cacheSize);
		if (candidateSize <= 0)
			throw new ArgumentOutOfRangeException(nameof(candidateSize), Resources.ParameterError.MustPositive);
		if (hitCountThreshold <= 0)
			throw new ArgumentOutOfRangeException(nameof(hitCountThreshold), Resources.ParameterError.MustPositive);
		if (candidateSize < 2 * cacheSize)
			throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(candidateSize));
		if (factory is null)
			throw new ArgumentNullException(nameof(factory));
		this.capacity = candidateSize;
		this.candidates = new(candidateSize);
		this.HitCountThreshold = hitCountThreshold;
		this.factory = factory;
	}

	/// <inheritdoc/>
	public readonly void Dispose()
	{
		this.cached.Dispose();
		this.candidates?.Clear();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private readonly bool AddInternal(in TKey key)
	{
		if (this.candidates.ContainsKey(key))
			return false;
		if (this.candidates.Count == this.capacity)
		{
			int minHit = int.MaxValue;
			TKey minKey = default!;
			foreach (var kv in this.candidates)
			{
				if (kv.Value < minHit)
				{
					minHit = kv.Value;
					minKey = kv.Key;
				}
			}
			if (!this.candidates.Remove(minKey))
				return false;
		}
		this.candidates.Add(key, 1);
		return true;
	}

	/// <summary>
	/// Add to the cacher if <paramref name="key"/> is not present.
	/// </summary>
	/// <param name="key">The key to add</param>
	/// <returns>Success or not.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Add(in TKey key)
	{
		lock (this.candidates)
		{
			return this.AddInternal(in key);
		}
	}

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool TryGetValue(in TKey key, [MaybeNullWhen(false)] out TValue value)
	{
		if (this.cached.TryGetValue(key, out value))
			return true;
		lock (this.candidates)
		{
			if (!this.candidates.TryGetValue(key, out int hitCount))
			{
				if (!this.AddInternal(in key))
					return false;
			}
			else
			{
				this.candidates[key] = ++hitCount;
			}
			if (!this.factory.Invoke(in key, out value))
				return false;
			return hitCount < this.HitCountThreshold || this.cached.Add(in key, value);
		}
	}

	/// <summary>
	/// Get the value of corresponding <paramref name="key"/>.
	/// </summary>
	/// <param name="key">The key used to get the value</param>
	/// <exception cref="KeyNotFoundException">If <paramref name="key"/> is not present in the underlying dictionary</exception>
	public readonly TValue this[in TKey key]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			if (!this.TryGetValue(key, out var value))
				throw new KeyNotFoundException();
			return value;
		}
	}
}