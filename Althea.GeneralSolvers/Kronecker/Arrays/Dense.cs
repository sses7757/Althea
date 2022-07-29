using System;

using Althea.Array;
using Althea.LinearAlgebra;
using Althea.Storage;


namespace Althea.GeneralSolvers.Kronecker.Array
{
	internal class DenseVector<T, TS> : Althea.Array.DenseVector<T, TS>, IConvertibleVector<T, DenseVector<T, TS>, DenseMatrix<T, TS>>
		where T : unmanaged, IBaseNumber<T>
		where TS : class, IStorage<T, TS>
	{
		#region create
		public DenseVector(Althea.Array.DenseVector<T, TS> @base) : base(@base)
		{
			// do nothing
		}
		#endregion

		#region convertible
		DenseMatrix<T, TS> IConvertibleVector<T, DenseVector<T, TS>, DenseMatrix<T, TS>>.ToMatrix(long rows)
		{
			if (this.Length % rows != 0)
				throw new ArgumentException(Resources.ArithmeticError.CannotDivide, nameof(rows));
			var output = this.ToCompact();
			return new(new Althea.Array.DenseMatrix<T, TS>(output, rows, this.Length / rows));
		}
		#endregion
	}

	internal class DenseMatrix<T, TS> : Althea.Array.DenseMatrix<T, TS>, IConvertibleMatrix<T, DenseMatrix<T, TS>, DenseVector<T, TS>>
		where T : unmanaged, IBaseNumber<T>
		where TS : class, IStorage<T, TS>
	{
		#region create
		public DenseMatrix(Althea.Array.DenseMatrix<T, TS> @base) : base(@base)
		{
			// do nothing
		}
		#endregion

		#region convertible
		DenseVector<T, TS> IConvertibleMatrix<T, DenseMatrix<T, TS>, DenseVector<T, TS>>.ToVector()
		{
			var output = this.ToCompact();
			return new(new Althea.Array.DenseVector<T, TS>(output, this.NRows * this.NCols));
		}

		static void IMatrixAddOperations<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.AddMatrices(DenseMatrix<T, TS>? A, T scalarA, DenseMatrix<T, TS>? B, T scalarB, DenseMatrix<T, TS> C, MatrixOperation opA, MatrixOperation opB) => DenseOperation<T, TS>.AddMatrices(A, scalarA, B, scalarB, C, opA, opB);

		static DenseMatrix<T, TS> IMatrixAddOperations<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.AddMatrices(DenseMatrix<T, TS>? A, T scalarA, DenseMatrix<T, TS>? B, T scalarB, MatrixOperation opA, MatrixOperation opB) => new(DenseOperation<T, TS>.AddMatrices(A, scalarA, B, scalarB, opA, opB));

		static void IMatrixMultiplyOperations<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.MultiplyMatries(DenseMatrix<T, TS> A, DenseMatrix<T, TS> B, T α, T β, DenseMatrix<T, TS> C, MatrixOperation opA, MatrixOperation opB) => DenseOperation<T, TS>.MultiplyMatries(A, B, α, β, C, opA, opB);

		static DenseMatrix<T, TS> IMatrixMultiplyOperations<T, DenseMatrix<T, TS>, DenseMatrix<T, TS>, DenseMatrix<T, TS>>.MultiplyMatries(DenseMatrix<T, TS> A, DenseMatrix<T, TS> B, T α, MatrixOperation opA, MatrixOperation opB) => new(DenseOperation<T, TS>.MultiplyMatries(A, B, α, opA, opB));
		#endregion

	}
}
