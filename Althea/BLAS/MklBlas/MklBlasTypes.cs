using System;
using System.Collections.Generic;
using System.Text;

namespace Althea.Blas.Mkl
{
	internal static class MklBlasExtension
	{
		internal static MatrixOperation ToMklMatrixOp(this Althea.MatrixOperation op)
		{
			return op switch
			{
				Althea.MatrixOperation.None => MatrixOperation.NoneTranspose,
				Althea.MatrixOperation.Transpose => MatrixOperation.Transpose,
				Althea.MatrixOperation.ConjugateTranspose => MatrixOperation.ConjugateTranspose,
				_ => throw new NotSupportedException("Other matrix operations" + Resource.BaseNotSupport)
			};
		}

		internal static MatrixFillMode ToMklFillMode(this Althea.MatrixFillMode op)
		{
			return op switch
			{
				Althea.MatrixFillMode.Upper => MatrixFillMode.Upper,
				Althea.MatrixFillMode.Lower => MatrixFillMode.Lower,
				_ => throw new NotSupportedException("Other fill modes" + Resource.BaseNotSupport)
			};
		}

		internal static SideMode ToMklSideMode(this Althea.SideMode side)
		{
			return side switch
			{
				Althea.SideMode.Left => SideMode.Left,
				Althea.SideMode.Right => SideMode.Right,
				_ => throw new NotSupportedException("Other side modes" + Resource.BaseNotSupport)
			};
		}

		internal static byte ToCharMatrixOp(this Althea.MatrixOperation op)
		{
			return op switch
			{
				Althea.MatrixOperation.None => (byte)'N',
				Althea.MatrixOperation.Transpose => (byte)'T',
				Althea.MatrixOperation.ConjugateTranspose => (byte)'C',
				_ => throw new NotSupportedException("Other matrix operations" + Resource.BaseNotSupport)
			};
		}
	}

	/// <summary>
	/// The matrix layout enum in MKL BLAS
	/// </summary>
	public enum MklBlasLayout
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

	/// <summary>
	/// The matrix transposition operation enum in MKL BLAS
	/// </summary>
	public enum MatrixOperation
	{
		/// <summary>
		/// Do not perform any transpositions 
		/// </summary>
		NoneTranspose = 111,
		/// <summary>
		/// Perform transposition
		/// </summary>
		Transpose = 112,
		/// <summary>
		/// Perform conjugate transposition
		/// </summary>
		ConjugateTranspose = 113
	};

	/// <summary>
	/// The symmetric/Hermitian matrix's storage mode in MKL BLAS
	/// </summary>
	public enum MatrixFillMode
	{
		/// <summary>
		/// The upper part is filled
		/// </summary>
		Upper = 121,
		/// <summary>
		/// The lower part is filled
		/// </summary>
		Lower = 122
	};

	/// <summary>
	/// The side mode in MKL BLAS
	/// </summary>
	public enum SideMode
	{
		/// <summary>
		/// Left
		/// </summary>
		Left = 141,
		/// <summary>
		/// Right
		/// </summary>
		Right = 142
	};
}
