global using System;
global using System.Collections.Generic;
global using System.Linq;

global using Althea.Numerics;

using System.Runtime.CompilerServices;
using System.Text;

[assembly: CLSCompliant(true)]


namespace Althea;

////₀ ₁ ₂ ₃ ₄ ₅ ₆ ₇ ₈ ₉ ₊ ₋ ₌ ₍ ₎ ₐ ₑ ₒ ₓ ₔ ₕ ₖ ₗ ₘ ₙ ₚ ₛ ₜ
////ᵃ ᵇ ᶜ ᵈ ᵉ ᵍ ʰ ⁱ ʲ ᵏ ˡ ᵐ ⁿ ᵒ ᵖ ᵒ ʳ ˢ ᵗ ᵘ ᵛ ʷ ˣ ʸ ᙆ ᴬ ᴮ ᒼ ᴰ ᴱ ᴳ ᴴ ᴵ ᴶ ᴷ ᴸ ᴹ ᴺ ᴼ ᴾ ᴼ̴ ᴿ ˢ ᵀ ᵁ ᵂ ˣ ᵞ ᙆ ꝰ ˀ ˁ ˤ ꟸ ꭜ ʱ ꭝ ꭞ ʴ ʵ ʶ ꭟ ˠ ꟹ ᴭ ᴯ ᴲ ᴻ ᴽ ᵄ ᵅ ᵆ ᵊ ᵋ ᵌ ᵑ ᵓ ᵚ ᵝ ᵞ ᵟ ᵠ ᵡ ᵎ ᵔ ᵕ ᵙ ᵜ ᶛ ᶜ ᶝ ᶞ ᶟ ᶡ ᶣ ᶤ ᶥ ᶦ ᶧ ᶨ ᶩ ᶪ ᶫ ᶬ ᶭ ᶮ ᶯ ᶰ ᶱ ᶲ ᶳ ᶴ ᶵ ᶶ ᶷ ᶸ ᶹ ᶺ ᶼ ᶽ ᶾ ᶿ ꚜ ꚝ ჼ ᒃ ᕻ ᑦ ᒄ ᕪ ᑋ ᑊ ᔿ ᐢ ᣕ ᐤ ᣖ ᣴ ᣗ ᔆ ᙚ ᐡ ᘁ ᐜ ᕽ ᙆ ᙇ ᒼ ᣳ ᒢ ᒻ ᔿ ᐤ ᣖ ᣵ ᙚ ᐪ ᓑ ᘁ ᐜ ᕽ ᙆ ᙇ ⁰ ¹ ² ³ ⁴ ⁵ ⁶ ⁷ ⁸ ⁹ ⁺ ⁻ ⁼ ⁽ ⁾ ˙ º

/// <summary>
/// The interface for an object whose validness can be checked
/// </summary>
public interface ICheckValid
{
	/// <summary>
	/// Check whether this object is a valid one or not
	/// </summary>
	/// <returns>The validness of this object</returns>
	bool IsValid();
}

/// <summary>
/// The interface for an object which can convert to string by principle string representation and string representation of properties
/// </summary>
/// <typeparam name="T">The actual implementing class/struct</typeparam>
public interface IMainPropertyFormattable<T> where T : IMainPropertyFormattable<T>
{
	/// <summary>
	/// Statically get the string representation of the principle value of <typeparamref name="T"/> as a <see cref="string"/>
	/// </summary>
	abstract static string StringMain { get; }

	/// <summary>
	/// Statically get the names of the printable properties of <typeparamref name="T"/> as a <see cref="IEnumerable{T}"/> of <see cref="string"/>
	/// </summary>
	abstract static IEnumerable<string> PropertyNames { get; }

	/// <summary>
	/// Get the values of printable properties of this object as a <see cref="IEnumerable{T}"/> of <see cref="string"/> whose order is the same as <see cref="PropertyNames"/>
	/// </summary>
	IEnumerable<object?> PropertyValues { get; }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string Combine(string main, IEnumerable<string> names, IEnumerable<object?> properties)
	{
		if (names is null || properties is null)
		{
			return main;
		}
		StringBuilder stringBuilder = new(main);
		if (properties.Any())
		{
			stringBuilder.Append(" { ");
			stringBuilder.Append(string.Join(", ", names.Zip(properties).Select(static p => $"{p.First} = {p.Second}")));
			stringBuilder.Append(" }");
		}
		return stringBuilder.ToString();
	}

	/// <summary>
	/// Get the string representation of this object by printing <see cref="StringMain"/>, <see cref="PropertyNames"/> and <see cref="PropertyValues"/>
	/// </summary>
	/// <param name="value">The instance value of type <typeparamref name="T"/></param>
	/// <returns>The string representation of <paramref name="value"/></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static string ToString(in T value) => Combine(T.StringMain, T.PropertyNames, value.PropertyValues);
}

/// <summary>
/// The interface for cloneable objects
/// </summary>
/// <typeparam name="T">The actual type that implements <see cref="ICloneable{T}"/></typeparam>
public interface ICloneable<T> : ICloneable where T : ICloneable<T>
{
	/// <summary>
	/// Creates a new object that is a copy of the current instance.
	/// </summary>
	/// <returns>A new object that is a copy of the current instance.</returns>
	new T Clone();

	object ICloneable.Clone() => this.Clone();
}

/// <summary>
/// The interface for objects that can create alike ones from
/// </summary>
/// <typeparam name="T">The actual type that implements <see cref="ICreateAlike{T}"/></typeparam>
public interface ICreateAlike<T> : ICloneable<T> where T : ICreateAlike<T>
{
	/// <summary>
	/// Creates a new object alike the current instance (with same meta data, etc.) while not copying the data.
	/// </summary>
	/// <returns>A new object alike the current instance.</returns>
	T CreateAlike();
}
