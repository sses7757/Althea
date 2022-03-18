using System;

using Althea.LinearAlgebra;
using Althea.LinearAlgebra.Dense;
using Althea.Storage;

using ExtBlas = Althea.LinearAlgebra.Dense.ExtendBlasApiSelector;


namespace Althea.Arrays.Matrices
{
	/// <summary>
	/// The dense matrix interface whose only storage is of type <typeparamref name="TS"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value <see cref="ISingleValueStorageArray{T, TS, TSelf}.Storage"/></typeparam>
	/// <typeparam name="TSelf">The concrete type that implements this <see cref="IDenseMatrix{T, TS, TSelf}"/></typeparam>
	public interface IDenseMatrix<T, TS, TSelf> : IBaseMatrix<T, TSelf>, ISingleValueStorageArray<T, TS, TSelf>
		where T : unmanaged, INumber<T>
		where TS : class, IStorage<T, TS>
		where TSelf : class, IDenseMatrix<T, TS, TSelf>
	{
		#region basic
		/// <summary>
		/// When implemented by a derived class, get the leading dimension of this matrix in <typeparamref name="T"/>.
		/// </summary>
		long LeadDim { get; }

		/// <summary>
		/// When implemented by a derived class, statically create a referenced <typeparamref name="TSelf"/> with given <paramref name="storage"/>, <paramref name="rows"/>, <paramref name="cols"/> and <paramref name="ld"/>.
		/// </summary>
		/// <param name="storage">The storage of the new matrix</param>
		/// <param name="rows">The number of rows in <typeparamref name="T"/> of the new matrix</param>
		/// <param name="cols">The number of columns in <typeparamref name="T"/> of the new matrix</param>
		/// <param name="ld">The leading dimension in <typeparamref name="T"/> of the new matrix, default 0 means the same as <paramref name="rows"/></param>
		/// <returns>The created referenced matrix of type <typeparamref name="TSelf"/>.</returns>
		protected abstract static TSelf CreateRef(TS storage, long rows, long cols, long ld = 0);

		TSelf IBaseMatrix<T, TSelf>.GetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol)
		{
			this.CheckRange(offsetRow, countRow, offsetCol, countCol);
			return TSelf.CreateRef(this.Storage + (offsetRow + offsetCol * this.LeadDim), countRow, countCol, this.LeadDim);
		}

		void IBaseMatrix<T, TSelf>.CopyTo(TSelf destination)
		{
			if (destination.NRows != this.NRows || destination.NCols != this.NCols)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(destination));
			this.Storage.Copy2DTo<T, TS, TS>(this.LeadDim, destination.Storage, destination.LeadDim, this.NRows, this.NCols);
		}

		void IBaseMatrix<T, TSelf>.SetSubmatrix(long offsetRow, long countRow, long offsetCol, long countCol, TSelf value)
		{
			this.CheckRange(offsetRow, countRow, offsetCol, countCol, value);
			var dst = this.Storage + (offsetRow + offsetCol * this.LeadDim);
			var src = value.Storage;
			src.Copy2DTo<T, TS, TS>(value.LeadDim, dst, this.LeadDim, countRow, countCol);
		}

		T IBaseMatrix<T, TSelf>.this[long x, long y]
		{
			get
			{
				this.CheckIndex(x, y);
				return (this.Storage + (x + y * this.LeadDim)).ToManaged<T, TS>();
			}
			set
			{
				this.CheckIndex(x, y);
				(this.Storage + (x + y * this.LeadDim)).FromManaged(value);
			}
		}
		#endregion

		#region point-wise operations
		void IValueArray<T, TSelf>.FillWith(T value) => ExtBlas.GeneralMatrixFill(this.Storage, this.LeadDim, value, this.NRows, this.NCols);

		void IValueArray<T, TSelf>.AddScalar(T value) => ExtBlas.GeneralMatrixAddScalar(this.Storage, this.LeadDim, value, this.NRows, this.NCols);

		void IValueArray<T, TSelf>.Scale(T value) => ExtBlas.GeneralMatricesAdd(MatrixOperation.None, MatrixOperation.None, this.NRows, this.NCols, value, this.Storage, this.LeadDim, T.Zero, (TS?)null, 1, this.Storage, this.LeadDim);

		void IValueArray<T, TSelf>.Conjugate() => ExtBlas.GeneralMatricesAdd(MatrixOperation.Conjugate, MatrixOperation.None, this.NRows, this.NCols, T.One, this.Storage, this.LeadDim, T.Zero, (TS?)null, 1, this.Storage, this.LeadDim);

		void IValueArray<T, TSelf>.Power(T power) => ExtBlas.GeneralMatrixPower(this.Storage, this.LeadDim, power, this.NRows, this.NCols);

		void IValueArray<T, TSelf>.Truncate(double threshold) => ExtBlas.GeneralMatrixTruncate<T, TS>(this.Storage, this.LeadDim, threshold, this.NRows, this.NCols);
		#endregion

		#region simple aggregation operations
		T IValueArray<T, TSelf>.Sum() => ExtBlas.GeneralMatrixSum<T, TS>(this.Storage, this.LeadDim, this.NRows, this.NCols);

		T IValueArray<T, TSelf>.AbsSum() => ExtBlas.GeneralMatrixAbsSum<T, TS>(this.Storage, this.LeadDim, this.NRows, this.NCols);

		T IValueArray<T, TSelf>.Norm() => ExtBlas.GeneralMatrixNorm<T, TS>(this.Storage, this.LeadDim, this.NRows, this.NCols);

		T IValueArray<T, TSelf>.ValueWithMaxAbs() => (this.Storage + ExtBlas.GeneralMatrixAbsArgMax<T, TS>(this.Storage, this.LeadDim, this.NRows, this.NCols)).ToManaged<T, TS>();

		T IValueArray<T, TSelf>.ValueWithMinAbs() => (this.Storage + ExtBlas.GeneralMatrixAbsArgMin<T, TS>(this.Storage, this.LeadDim, this.NRows, this.NCols)).ToManaged<T, TS>();
		#endregion
	}
}
