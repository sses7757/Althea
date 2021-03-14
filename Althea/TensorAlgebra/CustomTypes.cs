using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Althea.TensorAlgebra
{
	/// <summary>
	/// Binary operations used by tensor point-wise binary operations
	/// </summary>
	public enum BinaryOperation : int
	{
		/// <summary>
		/// Addition of two elements
		/// </summary>
		Addition,
		/// <summary>
		/// Multiplication of two elements
		/// </summary>
		Multiply,
		/// <summary>
		/// Maximum of two elements
		/// </summary>
		Maximum,
		/// <summary>
		/// Minimum of two elements
		/// </summary>
		Mininum
	}

	/// <summary>
	/// Unitary operations of tensor point-wise unary operations
	/// </summary>
	public enum UnaryOperation : int
	{
		/// <summary>
		/// Identity operator (i.e., elements are not changed)
		/// </summary>
		Identity,
		/// <summary>
		/// Complex conjugate
		/// </summary>
		Conjugate,
		/// <summary>
		/// Absolute value
		/// </summary>
		Abs,
		/// <summary>
		/// Negation
		/// </summary>
		Negate
	}
}
