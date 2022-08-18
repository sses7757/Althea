using System.Runtime.CompilerServices;
using System.Threading;

namespace Althea.Helpers;

/// <summary>
/// The structure for thread-safe limited-sized (with or without candidates) cacher based on <see cref="Dictionary{TKey, TValue}"/> and <see cref="Queue{T}"/>.
/// </summary>
/// <typeparam name="TKey">The type of keys, must be a value type</typeparam>
/// <typeparam name="TValue">The type of values, must be <see cref="IDisposable"/></typeparam>
public readonly struct LimitSizedCacher<TKey, TValue> : IDictionary<TKey, TValue>
	where TKey : struct, IEquatable<TKey>
	where TValue : notnull, IDisposable
{
	private readonly ReaderWriterLockSlim baseLocker = new();

	private readonly Dictionary<TKey, int>? candidates;
	private readonly Queue<TKey>? candidatesQueue;

	private readonly Dictionary<TKey, (TValue value, ReaderWriterLockSlim locker)> cached;
	private readonly Queue<TKey>? cachedQueue;

	/// <summary>
	/// Create a new <see cref="LimitSizedCacher{TKey, TValue}"/> with candidates size = 128 and cached size = 16
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public LimitSizedCacher()
	{
		this.candidates = new(128);
		this.candidatesQueue = new(128);
		this.cached = new(16);
		this.cachedQueue = new(16);
	}
}
