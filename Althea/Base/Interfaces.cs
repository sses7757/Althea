using System.Text;
using System.Collections.Generic;


namespace Althea
{
	/// <summary>
	/// The interface for a printable object
	/// </summary>
	public interface IPrintable
	{
		/// <summary>
		/// Get the string representation of the principle value of this object as a <see cref="string"/>
		/// </summary>
		string PrintableMain { get; }

		/// <summary>
		/// Get the string representation of printable properties of this object as a <see cref="IReadOnlyDictionary{TKey, TValue}"/>
		/// </summary>
		IReadOnlyDictionary<string, string> PrintableProperties { get; }

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
			StringBuilder stringBuilder = new StringBuilder(main);
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
		/// Get the string representation of this object by printing <see cref="PrintableMain"/>, <see cref="PrintableProperties"/>
		/// </summary>
		/// <returns>The string representation of this object</returns>
		string ToString() => Combine(this.PrintableMain, this.PrintableProperties);

		/// <summary>
		/// Get the string representation of this object by printing not only <see cref="PrintableMain"/> and <see cref="PrintableProperties"/> but also <paramref name="otherProperties"/>
		/// </summary>
		/// <param name="otherProperties">The string representation of other printable properties as a <see cref="IReadOnlyDictionary{TKey, TValue}"/></param>
		/// <returns>The combined string representation</returns>
		string ToString(IReadOnlyDictionary<string, string> otherProperties) => Combine(this.PrintableMain, this.PrintableProperties, otherProperties);
	}
}
