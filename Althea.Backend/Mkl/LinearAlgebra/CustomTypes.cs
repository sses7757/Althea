using Althea.LinearAlgebra;


namespace Althea.Backend.Mkl.LinearAlgebra
{
	/// <summary>
	/// The supplement <see cref="UnaryOperation"/>s.
	/// </summary>
	public enum UnaryOperationSupplement
	{
		/// <summary>
		/// Operation that returns the base <c>e</c> exponential of the input parameter
		/// </summary>
		Exp = UnaryOperation.AbsoluteValue + 1,
		/// <summary>
		/// Operation that returns the base 2 exponential of the input parameter
		/// </summary>
		Exp2,
		/// <summary>
		/// Operation that returns the base 10 exponential of the input parameter
		/// </summary>
		Exp10,
		/// <summary>
		/// Operation that returns the base <c>e</c> exponential of the input parameter minus 1
		/// </summary>
		ExpM1,
		/// <summary>
		/// Operation that returns the base <c>e</c> logarithm of the input parameter
		/// </summary>
		Ln,
		/// <summary>
		/// Operation that returns the base 2 logarithm of the input parameter
		/// </summary>
		Log2,
		/// <summary>
		/// Operation that returns the base 10 logarithm of the input parameter
		/// </summary>
		Log10,
		/// <summary>
		/// Operation that returns the base <c>e</c> logarithm of the input parameter plus 1
		/// </summary>
		Log1p,
		/// <summary>
		/// Operation that returns the exponent part of the input parameter
		/// </summary>
		LogBinary,
		/// <summary>
		/// Operation that returns the cosine of the input parameter
		/// </summary>
		Cos,
		/// <summary>
		/// Operation that returns the sine of the input parameter
		/// </summary>
		Sin,
		/// <summary>
		/// Operation that returns the tangent of the input parameter
		/// </summary>
		Tan,
		/// <summary>
		/// Operation that returns the inverse cosine of the input parameter
		/// </summary>
		ArcCos,
		/// <summary>
		/// Operation that returns the inverse sine of the input parameter
		/// </summary>
		ArcSin,
		/// <summary>
		/// Operation that returns the inverse tangent of the input parameter
		/// </summary>
		ArcTan,
		/// <summary>
		/// Operation that returns the hyperbolic cosine of the input parameter
		/// </summary>
		Cosh,
		/// <summary>
		/// Operation that returns the hyperbolic sine of the input parameter
		/// </summary>
		Sinh,
		/// <summary>
		/// Operation that returns the hyperbolic tangent of the input parameter
		/// </summary>
		Tanh,
		/// <summary>
		/// Operation that returns the inverse hyperbolic cosine of the input parameter
		/// </summary>
		ArcCosh,
		/// <summary>
		/// Operation that returns the inverse hyperbolic sine of the input parameter
		/// </summary>
		ArcSinh,
		/// <summary>
		/// Operation that returns the inverse hyperbolic tangent of the input parameter
		/// </summary>
		ArcTanh,
	}
}
