using System;
using System.Text;
using System.Collections.Generic;

[assembly: CLSCompliant(true)]


namespace Althea
{
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
	public interface IMainPropertyFormat
	{
		/// <summary>
		/// Get the string representation of the principle value of this object as a <see cref="string"/>
		/// </summary>
		string StringMain { get; }

		/// <summary>
		/// Get the string representation of printable properties of this object as a <see cref="IReadOnlyDictionary{TKey, TValue}"/>
		/// </summary>
		IReadOnlyDictionary<string, string> StringProperties { get; }

		/// <summary>
		/// Combine the string representation of the principle value (<paramref name="main"/>) and the string representation of printable properties (<paramref name="properties"/>)
		/// </summary>
		/// <param name="main">The string representation of the principle value</param>
		/// <param name="properties">The string representation of printable properties</param>
		/// <returns>The combination <see cref="string"/></returns>
		public static string Combine(string main, params IReadOnlyDictionary<string, string>[] properties)
		{
			if (properties is null || properties.Length == 0)
			{
				return main;
			}
			IEnumerable<KeyValuePair<string, string>> props = properties[0];
			for (int i = 1; i < properties.Length; i++)
			{
				props = System.Linq.Enumerable.Concat(props, properties[i]);
			}
			StringBuilder stringBuilder = new(main);
			stringBuilder.Append(' ').Append('[');
			foreach (var item in props)
			{
				stringBuilder.Append(item.Key).Append('=').Append(item.Value);
				stringBuilder.Append(',').Append(' ');
			}
			stringBuilder.Remove(stringBuilder.Length - 2, 2);
			stringBuilder.Append(']');
			return stringBuilder.ToString();
		}

		/// <summary>
		/// Get the string representation of this object by printing <see cref="StringMain"/>, <see cref="StringProperties"/>
		/// </summary>
		/// <returns>The string representation of this object</returns>
		string? ToString() => Combine(this.StringMain, this.StringProperties);

		/// <summary>
		/// Get the string representation of this object by printing not only <see cref="StringMain"/> and <see cref="StringProperties"/> but also <paramref name="otherProperties"/>
		/// </summary>
		/// <param name="otherProperties">The string representation of other printable properties as a <see cref="IReadOnlyDictionary{TKey, TValue}"/></param>
		/// <returns>The combined string representation</returns>
		string ToString(IReadOnlyDictionary<string, string> otherProperties) => Combine(this.StringMain, this.StringProperties, otherProperties);
	}

	/// <summary>
	/// The interface for a cloneable object
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface ICloneable<T> : ICloneable where T : ICloneable<T>
	{
		/// <summary>
		/// Creates a new object that is a copy of the current instance.
		/// </summary>
		/// <returns>A new object that is a copy of the current instance</returns>
		new T Clone();

		object ICloneable.Clone() => this.Clone();
	}
}
