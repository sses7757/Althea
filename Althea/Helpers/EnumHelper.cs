using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading;

namespace Althea.Helpers;

#region debug
internal readonly struct DebugManagedEnum<T> where T : unmanaged, Enum
{
	public readonly T Value { get; }

	public readonly string Name { get; }

	internal DebugManagedEnum(ManagedEnum<T> value)
	{
		this.Value = value.Value;
		this.Name = value.ToString();
	}
}
#endregion

/// <summary>
/// The managed enum struct that provide safe extend definitions for existing enum type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The enum type</typeparam>
public readonly record struct ManagedEnum<T> where T : unmanaged, Enum
{
	#region instance
	/// <summary>
	/// The underlying enum value of type <typeparamref name="T"/>
	/// </summary>
	[JsonIgnore]
	public readonly T Value { get; }

	/// <summary>
	/// The default constructor
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ManagedEnum(T value) => this.Value = value;

	[JsonInclude]
	private readonly string[] Flags
	{
		get
		{
			if (!IS_FLAG)
				return new[] { this.ToStringInternal(out _) };
			List<string> names = new((int)MAX_VALUE);
			long value = GetIntValue(this.Value);
			for (byte i = 0; i < MAX_VALUE; i++)
			{
				if (!value.IsBitSet(i))
					continue;
				long v = 1L << i;
				ManagedEnum<T> flag = Unsafe.As<long, T>(ref v);
				string sub = flag.ToStringInternal(out bool success);
				if (!success)
					return new[] { this.ToStringInternal(out _) };
				names.Add(sub);
			}
			return names.ToArray();
		}
	}

	[JsonConstructor]
	internal ManagedEnum(string[] flags)
	{
		locker.EnterReadLock();
		try
		{
			long value = 0;
			for (int i = 0; i < flags.Length; i++)
			{
				if (!TryParseInternal(flags[i], out var sub))
					return;
				value += GetIntValue(sub.Value);
			}
			this.Value = Unsafe.As<long, T>(ref value);
		}
		finally
		{
			locker.ExitReadLock();
		}
	}

	/// <summary>
	/// Implicitly convert a <see cref="ManagedEnum{T}"/> to its enum <see cref="Value"/>.
	/// </summary>
	public static implicit operator T(ManagedEnum<T> value) => value.Value;

	/// <summary>
	/// Implicitly convert a enum <paramref name="value"/> to a <see cref="ManagedEnum{T}"/>.
	/// </summary>
	public static implicit operator ManagedEnum<T>(T value) => new(value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private readonly string ToStringInternal(out bool success)
	{
		success = true;
		if (Enum.GetName(this.Value) is { } str)
			return str;
		if (names.TryGetValue(this.Value, out str))
			return str;
		success = false;
		return $"Undefined Value {this.Value}";
	}

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override readonly string ToString() => string.Join(" + ", this.Flags);
	#endregion

	#region static
	private static readonly ReaderWriterLockSlim locker = new();
	private static readonly Dictionary<T, string> names = new();
	private static readonly Dictionary<string, T> namesInv = new();
	private static readonly long MAX_VALUE;
	private static long current;
	private static readonly bool IS_FLAG;

	static ManagedEnum()
	{
		IS_FLAG = typeof(T).CustomAttributes.Any(static attr => attr.AttributeType == typeof(FlagsAttribute));
		MAX_VALUE = IS_FLAG ? GetMaxValue() : (long)GetMaxValueFlag();
		current = Enum.GetValues<T>().Select(static v => GetIntValue(v)).Max();
		if (IS_FLAG)
			current = long.Log2(current);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static long GetIntValue(T value)
	{
		return Type.GetTypeCode(typeof(T)) switch
		{
			TypeCode.SByte => Unsafe.As<T, sbyte>(ref value),
			TypeCode.Byte => Unsafe.As<T, byte>(ref value),
			TypeCode.Int16 => Unsafe.As<T, short>(ref value),
			TypeCode.UInt16 => Unsafe.As<T, ushort>(ref value),
			TypeCode.Char => Unsafe.As<T, char>(ref value),
			TypeCode.UInt32 => Unsafe.As<T, uint>(ref value),
			TypeCode.Int32 => Unsafe.As<T, int>(ref value),
			TypeCode.UInt64 => (long)Unsafe.As<T, ulong>(ref value),
			TypeCode.Int64 => Unsafe.As<T, long>(ref value),
			_ => throw new InvalidOperationException(Resources.ParameterError.UnexpectedType),
		};
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static long GetMaxValue()
	{
		return Type.GetTypeCode(typeof(T)) switch
		{
			TypeCode.SByte => sbyte.MaxValue,
			TypeCode.Byte => byte.MaxValue,
			TypeCode.Int16 => short.MaxValue,
			TypeCode.UInt16 => ushort.MaxValue,
			TypeCode.Char => char.MaxValue,
			TypeCode.UInt32 => uint.MaxValue,
			TypeCode.Int32 => int.MaxValue,
			TypeCode.UInt64 => long.MaxValue,
			TypeCode.Int64 => long.MaxValue,
			_ => throw new InvalidOperationException(Resources.ParameterError.UnexpectedType),
		};
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ulong GetMaxValueFlag()
	{
		return Type.GetTypeCode(typeof(T)) switch
		{
			TypeCode.SByte => ulong.Log2(byte.MaxValue),
			TypeCode.Byte => ulong.Log2(byte.MaxValue),
			TypeCode.Int16 => ulong.Log2(ushort.MaxValue),
			TypeCode.UInt16 => ulong.Log2(ushort.MaxValue),
			TypeCode.Char => ulong.Log2(char.MaxValue),
			TypeCode.UInt32 => ulong.Log2(uint.MaxValue),
			TypeCode.Int32 => ulong.Log2(uint.MaxValue),
			TypeCode.UInt64 => ulong.Log2(ulong.MaxValue),
			TypeCode.Int64 => ulong.Log2(ulong.MaxValue),
			_ => throw new InvalidOperationException(Resources.ParameterError.UnexpectedType),
		};
	}

	/// <summary>
	/// Declare a new enum of type <typeparamref name="T"/> with given <paramref name="name"/>/string representation.
	/// </summary>
	/// <param name="name">The name / string representation of the newly declared enum</param>
	/// <exception cref="InvalidOperationException">If the enum is fully declared thus has no room for new ones</exception>
	/// <exception cref="ArgumentException">If <paramref name="name"/> is null or empty or it contains space</exception>
	public static ManagedEnum<T> DeclareNewEnum(string name)
	{
		if (string.IsNullOrEmpty(name) || name.Contains(' '))
			throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(name));
		locker.EnterWriteLock();
		try
		{
			if (current == MAX_VALUE)
				throw new InvalidOperationException(Resources.ParameterError.DuplicateValue);
			current++;
			T newValue;
			if (IS_FLAG)
			{
				long val = 1L << (int)current;
				newValue = Unsafe.As<long, T>(ref val);
			}
			else
			{
				newValue = Unsafe.As<long, T>(ref current);
			}
			names.Add(newValue, name);
			namesInv.Add(name, newValue);
			return newValue;
		}
		finally
		{
			locker.ExitWriteLock();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryParseInternal(string name, out ManagedEnum<T> e)
	{
		e = default;
		if (Enum.TryParse(name, out T value))
		{
			e = value; return true;
		}
		if (namesInv.TryGetValue(name, out value))
		{
			e = value; return true;
		}
		return false;
	}

	/// <summary>
	/// Tries to parse the given enum <paramref name="name"/> to a enum value.
	/// </summary>
	/// <param name="e">The output value of the <see cref="ManagedEnum{T}"/></param>
	/// <param name="name">The name / string representation of the enum to parse</param>
	/// <returns>Success or not</returns>
	public static bool TryParse(string name, out ManagedEnum<T> e)
	{
		locker.EnterReadLock();
		try
		{
			if (TryParseInternal(name, out e))
				return true;
			if (!IS_FLAG)
				return false;
			string[] flags = name[1..^1].Split(" + ");
			long value = 0;
			for (int i = 0; i < flags.Length; i++)
			{
				if (!TryParseInternal(flags[i], out var sub))
					return false;
				value += GetIntValue(sub.Value);
			}
			e = Unsafe.As<long, T>(ref value);
			return true;
		}
		finally
		{
			locker.ExitReadLock();
		}
	}

	/// <summary>
	/// Parse the given enum <paramref name="name"/> to a enum value
	/// </summary>
	/// <param name="name">The name / string representation of the enum to parse</param>
	/// <returns>The <see cref="ManagedEnum{T}"/> given by <paramref name="name"/></returns>
	/// <exception cref="ArgumentException">If <paramref name="name"/> cannot be converted to a enum of type <typeparamref name="T"/></exception>
	public static ManagedEnum<T> Parse(string name)
	{
		if (!TryParse(name, out var e))
			throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(name));
		return e;
	}
	#endregion
}
