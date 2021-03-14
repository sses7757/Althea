using System;
using System.Collections.Generic;
using System.Text;

namespace Althea.Solver.Mkl
{
	internal static class MklBlasExtension
	{
		internal static sbyte ToChar(this EigMode mode)
		{
			return mode switch
			{
				EigMode.NoVector => (sbyte)'N',
				EigMode.Vector => (sbyte)'V',
				EigMode.LeftOnly => (sbyte)'V',
				EigMode.RightOnly => (sbyte)'V',
				_ => default,
			};
		}

		internal static sbyte ToChar(this MatrixFillMode mode)
		{
			return mode switch
			{
				MatrixFillMode.Upper => (sbyte)'U',
				MatrixFillMode.Lower => (sbyte)'L',
				_ => default,
			};
		}
	}

	/// <summary>
	/// The matrix layout enum in MKL Solver
	/// </summary>
	public enum MklSolverLayout
	{
		/// <summary>
		/// Row major storage layout
		/// </summary>
		RowMajor = 101,
		/// <summary>
		/// Column major storage layout
		/// </summary>
		ColMajor = 102
	};
}
