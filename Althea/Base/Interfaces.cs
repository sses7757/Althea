using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;

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
		/// Get the string representation of printable properties of this object as a <see cref="IEnumerable{T}"/> of <see cref="KeyValuePair{TKey, TValue}"/>
		/// </summary>
		IEnumerable<KeyValuePair<string, object?>> StringProperties { get; }

		/// <summary>
		/// Combine the string representation of the principle value (<paramref name="main"/>) and the string representation of printable properties (<paramref name="properties"/>)
		/// </summary>
		/// <param name="main">The string representation of the principle value</param>
		/// <param name="properties">The string representation of printable properties</param>
		/// <returns>The combination <see cref="string"/></returns>
		public static string Combine(string main, IEnumerable<KeyValuePair<string, object?>> properties)
		{
			if (properties is null)
			{
				return main;
			}
			StringBuilder stringBuilder = new(main);
			if (properties.Any())
			{
				stringBuilder.Append(' ').Append('[');
				stringBuilder.Append(string.Join(", ", properties.Select(static p => $"{p.Key}={p.Value}")));
				stringBuilder.Append(']');
			}
			return stringBuilder.ToString();
		}

		/// <summary>
		/// Get the string representation of this object by printing <see cref="StringMain"/>, <see cref="StringProperties"/>
		/// </summary>
		/// <returns>The string representation of this object</returns>
		string ToString() => Combine(this.StringMain, this.StringProperties);
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

	/// <summary>
	/// The interface used to set the back-end implementations all at once
	/// </summary>
	public interface ISetBackend
	{
		/// <summary>
		/// Check whether all the back-end implementations are available
		/// </summary>
		bool Available { get; }

		/// <summary>
		/// The implementation type of <see cref="Althea.Storage.AbstractApi"/>
		/// </summary>
		Type StorageImplementation { get; }

		/// <summary>
		/// The implementation type of <see cref="Althea.LinearAlgebra.Dense.AbstractApi"/>
		/// </summary>
		Type DenseLinearAlgebraImplementation { get; }

		/// <summary>
		/// The implementation type of <see cref="Althea.LinearAlgebra.Sparse.AbstractApi"/>
		/// </summary>
		Type SparseLinearAlgebraImplementation { get; }

		/// <summary>
		/// The implementation type of <see cref="Althea.TensorAlgebra.Dense.AbstractApi"/>
		/// </summary>
		Type DenseTensorAlgebraImplementation { get; }

		/// <summary>
		/// The implementation type of <see cref="Althea.TensorAlgebra.Sparse.AbstractApi"/>
		/// </summary>
		Type SparseTensorAlgebraImplementation { get; }

		/// <summary>
		/// The implementation type of <see cref="Althea.Statistics.AbstractApi"/>
		/// </summary>
		Type StatisticsImplementation { get; }

		/// <summary>
		/// The implementation type of <see cref="Althea.Solver.AbstractApi"/>
		/// </summary>
		Type SolverImplementation { get; }
	}
}
