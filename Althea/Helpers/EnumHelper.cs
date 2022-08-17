using System.Numerics;

using Althea.Linq;


namespace Althea.Helpers;

/// <summary>
/// The read-only struct for storing method parameter types
/// </summary>
public readonly struct MethodParametersInfo : IEqualityOperators<MethodParametersInfo, MethodParametersInfo, bool>
{
	private readonly FixedClassBuffer_8<Type> parameterTypes;

	/// <summary>
	/// Create an <see cref="MethodParametersInfo"/> from <paramref name="parameterInfos"/>
	/// </summary>
	/// <param name="parameterInfos">The input parameters types as a <see cref="ReadOnlySpan{T}"/> of <see cref="System.Reflection.ParameterInfo"/></param>
	/// <exception cref="ArgumentNullException">If <paramref name="parameterInfos"/> is empty</exception>
	/// <exception cref="ArgumentException">If <paramref name="parameterInfos"/> is too long</exception>
	public MethodParametersInfo(ReadOnlySpan<System.Reflection.ParameterInfo> parameterInfos)
	{
		if (parameterInfos.IsEmpty)
			throw new ArgumentNullException(nameof(parameterInfos));
		if (parameterInfos.Length > 8)
			throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(parameterInfos));
		Span<Type> types = (stackalloc IntPtr[parameterInfos.Length]).AsClassType<Type>();
		for (int i = 0; i < parameterInfos.Length; i++)
			types[i] = parameterInfos[i].ParameterType;
		this.parameterTypes = new(types);
	}

	/// <summary>
	/// Create an <see cref="MethodParametersInfo"/> from <paramref name="parameterTypes"/>
	/// </summary>
	/// <param name="parameterTypes">The input parameters types as a <see cref="ReadOnlySpan{T}"/> of <see cref="Type"/></param>
	/// <exception cref="ArgumentNullException">If <paramref name="parameterTypes"/> is empty</exception>
	/// <exception cref="ArgumentException">If <paramref name="parameterTypes"/> is too long</exception>
	public MethodParametersInfo(ReadOnlySpan<Type> parameterTypes)
	{
		if (parameterTypes.IsEmpty)
			throw new ArgumentNullException(nameof(parameterTypes));
		if (parameterTypes.Length > 8)
			throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(parameterTypes));
		this.parameterTypes = new(parameterTypes);
	}

	/// <summary>
	/// Indicates whether the current object is equal to another object of the same type.
	/// </summary>
	/// <param name="other">The other <see cref="MethodParametersInfo"/> to compare with this one.</param>
	/// <returns>True if the current object is equal to the other parameter; otherwise, false.</returns>
	public bool Equals(MethodParametersInfo other) => this.parameterTypes == other.parameterTypes;

	/// <summary>
	/// Indicates whether the current object is equal to another object of the same type.
	/// </summary>
	/// <param name="obj">The other object to compare with this one.</param>
	/// <returns>True if the current object is equal to the other parameter; otherwise, false.</returns>
	public override bool Equals(object? obj) => obj is MethodParametersInfo info && this.Equals(info);

	/// <summary>
	/// Get the hash code of this <see cref="MethodParametersInfo"/>
	/// </summary>
	/// <returns>The hash code of this <see cref="MethodParametersInfo"/></returns>
	public override int GetHashCode() => this.parameterTypes.GetHashCode();

	/// <summary>
	/// Equality operator
	/// </summary>
	public static bool operator ==(MethodParametersInfo left, MethodParametersInfo right) => left.Equals(right);

	/// <summary>
	/// Inequality operator
	/// </summary>
	public static bool operator !=(MethodParametersInfo left, MethodParametersInfo right) => !left.Equals(right);
}

/// <summary>
/// The static class for generic enum helper methods
/// </summary>
public static class EnumHelper
{
	private static class NameCacher<T> where T : struct, Enum
	{
		internal static Dictionary<T, string> names = new();

		internal static Dictionary<MethodParametersInfo, Type> methodParas = new();
	}

	private static class MethodCacher<TEnum, TDelegate> where TEnum : struct, Enum where TDelegate : Delegate
	{
		internal static Dictionary<TEnum, TDelegate> methods = new();
	}

	/// <summary>
	/// Get the name / string representation of the given enum value
	/// </summary>
	/// <typeparam name="T">The type of the enum</typeparam>
	/// <param name="e">The value of the enum</param>
	/// <returns><paramref name="e"/>'s name / string representation</returns>
	public static string GetName<T>(this T e) where T : struct, Enum
	{
		if (NameCacher<T>.names.TryGetValue(e, out string? name))
			return name;
		else
			return e.ToString();
	}

	/// <summary>
	/// Set the name / string representation of the given enum value
	/// </summary>
	/// <typeparam name="T">The type of the enum</typeparam>
	/// <param name="e">The value of the enum</param>
	/// <param name="name">The name / string representation of the given enum value <paramref name="e"/></param>
	/// <exception cref="InvalidOperationException">If <paramref name="e"/> is already defined</exception>
	/// <exception cref="ArgumentException">If <paramref name="name"/> is null or empty or it contains space</exception>
	public static void SetName<T>(this T e, string name) where T : struct, Enum
	{
		if (Enum.IsDefined(e))
			throw new InvalidOperationException(Resources.ParameterError.InvalidValue);
		if (string.IsNullOrEmpty(name) || name.Contains(' '))
			throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(name));
		NameCacher<T>.names[e] = name;
	}

	/// <summary>
	/// Tries to parse the given enum <paramref name="name"/> to a enum value
	/// </summary>
	/// <typeparam name="T">The type of the enum</typeparam>
	/// <param name="e">The output value of the enum</param>
	/// <param name="name">The name / string representation of the enum to parse</param>
	/// <returns>Success or not</returns>
	public static bool TryParse<T>(string name, out T e) where T : struct, Enum
	{
		if (Enum.TryParse(name, out e))
			return true;
		if (!NameCacher<T>.names.ContainsValue(name))
			return false;
		e = NameCacher<T>.names.FirstOrDefault(kv => kv.Value == name).Key;
		return true;
	}

	/// <summary>
	/// Parse the given enum <paramref name="name"/> to a enum value
	/// </summary>
	/// <typeparam name="T">The type of the enum</typeparam>
	/// <param name="name">The name / string representation of the enum to parse</param>
	/// <returns>The enum of type <typeparamref name="T"/> given by <paramref name="name"/></returns>
	/// <exception cref="ArgumentException">If <paramref name="name"/> cannot be converted to a enum of type <typeparamref name="T"/></exception>
	public static T Parse<T>(string name) where T : struct, Enum
	{
		if (Enum.TryParse(name, out T e))
			return e;
		if (!NameCacher<T>.names.ContainsValue(name))
			throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(name));
		e = NameCacher<T>.names.FirstOrDefault(kv => kv.Value == name).Key;
		return e;
	}

	/// <summary>
	/// Get the method delegate of type <typeparamref name="TDelegate"/> associated to the given enum value
	/// </summary>
	/// <typeparam name="TEnum">The type of the enum</typeparam>
	/// <typeparam name="TDelegate">The type of the delegate of the method</typeparam>
	/// <param name="e">The value of the enum</param>
	/// <returns><paramref name="e"/>'s associated method delegate, null for not existing</returns>
	public static TDelegate? GetMethod<TEnum, TDelegate>(this TEnum e) where TEnum : struct, Enum where TDelegate : Delegate
	{
		if (!MethodCacher<TEnum, TDelegate>.methods.TryGetValue(e, out TDelegate? method))
			method = null;
		return method;
	}

	/// <summary>
	/// Set the method delegate of type <typeparamref name="TDelegate"/> associated to the given enum value
	/// </summary>
	/// <typeparam name="TEnum">The type of the enum</typeparam>
	/// <typeparam name="TDelegate">The type of the delegate of the method</typeparam>
	/// <param name="e">The value of the enum</param>
	/// <param name="method">The delegate of the method to set</param>
	/// <exception cref="ArgumentException">If there are exited delegates whose parameters are the same as <typeparamref name="TDelegate"/>'s while <typeparamref name="TDelegate"/> != that delegate</exception>
	public static void SetMethod<TEnum, TDelegate>(this TEnum e, TDelegate method) where TEnum : struct, Enum where TDelegate : Delegate
	{
		if (MethodCacher<TEnum, TDelegate>.methods.Count != 0)
		{
			MethodCacher<TEnum, TDelegate>.methods[e] = method;
			return;
		}
		if (!NameCacher<TEnum>.methodParas.TryGetValue(new(method.Method.GetParameters()), out Type? oldType))
			oldType = null;
		if (oldType is null || oldType == typeof(TDelegate))
		{
			MethodCacher<TEnum, TDelegate>.methods[e] = method;
			return;
		}
	}
}
