using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Althea.Linq;
using Althea.Arrays;
using Althea.LinearAlgebra.Sparse;


namespace Althea.Backend.Arrays
{
	/// <summary>
	/// The concrete (non-blocked) sparse matrix class with the only mutable <see cref="ValueArray{T}.Storage"/> that refers to the value array storage and the <see cref="SparseMatrix{T, TInd}.RowIndexStorage"/> and <see cref="SparseMatrix{T, TInd}.ColIndexStorage"/> that refer to the <b>sorted</b> row and column index arrays' storages.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct that implements <see cref="IFormattable"/> and <see cref="IEquatable{T}"/> as the data type</typeparam>
	/// <typeparam name="TInd">Any integer-typed unmanaged struct as the index type</typeparam>
	public class SparseMatrix<T, TInd> : AbstractSparseMatrix<T, TInd>
		where T : unmanaged, IFormattable, IEquatable<T>
		where TInd : unmanaged
	{
		#region basic
		/// <summary>
		/// Get the storage of the row index array of this sparse matrix as a <see cref="Storage{T}"/> of <typeparamref name="TInd"/>
		/// </summary>
		public Storage<TInd> RowIndexStorage => this.m_rowIndexArray;

		/// <summary>
		/// Get the storage of the column index array of this sparse matrix as a <see cref="Storage{T}"/> of <typeparamref name="TInd"/>
		/// </summary>
		public Storage<TInd> ColIndexStorage => this.m_colIndexArray;

		/// <summary>
		/// Create an empty <see cref="SparseMatrix{T, TInd}"/>
		/// </summary>
		public SparseMatrix() : base(0, 0, Storage<T>.Empty, Storage<TInd>.Empty, Storage<TInd>.Empty, SparseMatrixFormat.COOR) { }

		/// <summary>
		/// Create a <see cref="SparseMatrix{T, TInd}"/> (of <see cref="SparseMatrixFormat.COOR"/>, <see cref="SparseMatrixFormat.COOC"/>, <see cref="SparseMatrixFormat.CSR"/> or <see cref="SparseMatrixFormat.CSC"/> format) with given size, <paramref name="valueArray"/> and index arrays.
		/// </summary>
		/// <param name="rows">The presenting number of rows of this matrix</param>
		/// <param name="cols">The presenting number of columns of this matrix</param>
		/// <param name="valueArray">The value array as a <see cref="Storage{T}"/> of <typeparamref name="T"/></param>
		/// <param name="rowIndexArray">The row index array as a <see cref="Storage{T}"/> of <typeparamref name="TInd"/></param>
		/// <param name="colIndexArray">The column index array as a <see cref="Storage{T}"/> of <typeparamref name="TInd"/></param>
		/// <param name="format">The atomic <see cref="SparseMatrixFormat"/> of a pre-defined value</param>
		/// <param name="defaultValue">The default value (the value not specified) of this sparse vector</param>
		/// <exception cref="NotSupportedException">If the <typeparamref name="TInd"/> is not an integral type</exception>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="format"/> is not atomic or not of allowed format</exception>
		/// <exception cref="ArgumentException">If the lengths of storages does not fit the underlying regulations indicated by <paramref name="format"/></exception>
		public SparseMatrix(long rows, long cols, Storage<T> valueArray, Storage<TInd> rowIndexArray, Storage<TInd> colIndexArray, SparseMatrixFormat format, T defaultValue = default) : base(rows, cols, valueArray, rowIndexArray, colIndexArray, format, defaultValue)
		{
			if (format < SparseMatrixFormat.COOR || format > SparseMatrixFormat.CSC)
				throw new ArgumentOutOfRangeException(nameof(format), format, Resources.Parameter.InvalidValue);
			if (!FitRegulation(rows, cols, valueArray.Length, rowIndexArray.Length, colIndexArray.Length, format))
				throw new ArgumentException(Resources.Parameter.WrongSize);
		}

		private static bool FitRegulation(long rows, long cols, long nonDefaults, long lengthRow, long lengthCol, SparseMatrixFormat format)
		{
			return format switch
			{
				SparseMatrixFormat.COOR or SparseMatrixFormat.COOC => lengthRow >= nonDefaults && lengthCol >= nonDefaults,
				SparseMatrixFormat.CSR => lengthRow >= rows + 1 && lengthCol >= nonDefaults,
				SparseMatrixFormat.CSC => lengthRow >= nonDefaults && lengthCol >= cols + 1,
				_ => false,
			};
		}

		private void GetRegulatedStorages(out Storage<TInd> row, out Storage<TInd> col)
		{
			switch (this.Format)
			{
				case SparseMatrixFormat.COOR:
				case SparseMatrixFormat.COOC:
					row = this.RowIndexStorage.MakeReference(newLength: this.ActualLength);
					col = this.ColIndexStorage.MakeReference(newLength: this.ActualLength);
					break;
				case SparseMatrixFormat.CSR:
					row = this.RowIndexStorage.MakeReference(newLength: this.NRows + 1);
					col = this.ColIndexStorage.MakeReference(newLength: this.ActualLength);
					break;
				case SparseMatrixFormat.CSC:
					row = this.RowIndexStorage.MakeReference(newLength: this.ActualLength);
					col = this.ColIndexStorage.MakeReference(newLength: this.NCols + 1);
					break;
				default: // never here
					throw new NotSupportedException();
			}
		}

		#endregion

		#region clone related
		/// <summary>
		/// Convert this sparse matrix to a dense matrix whose <see cref="Storage{T}"/> is <paramref name="denseStorage"/>
		/// </summary>
		/// <param name="denseStorage">The <see cref="Storage{T}"/> of the dense matrix to overwrite</param>
		/// <param name="leadDim">The leading dimension of the target dense matrix</param>
		/// <param name="rows">The number of rows of the dense matrix</param>
		/// <param name="cols">The number of columns of the dense matrix</param>
		/// <exception cref="ArgumentNullException">If <paramref name="denseStorage"/> is null or invalid</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="rows"/> or <paramref name="cols"/> or <paramref name="leadDim"/> is less than 1</exception>
		/// <exception cref="ArgumentException">If <paramref name="rows"/> &gt; <paramref name="leadDim"/> or <paramref name="leadDim"/> * <paramref name="cols"/> &gt; <paramref name="denseStorage"/>.<see cref="Storage{T}.Length">Length</see></exception>
		public override void ToDense(Storage<T> denseStorage, long leadDim, long rows, long cols)
		{

		}

		/// <summary>
		/// Deep clone the sparse matrix, the mutable status will not be copied.
		/// </summary>
		/// <returns>The cloned sparse matrix</returns>
		public override SparseMatrix<T, TInd> Clone()
		{
			Span<IntPtr> temp = stackalloc IntPtr[2];
			var span = ((ISparseArray<T, TInd>)this).NewArraysAlike(out ActualStorage<T> value, temp, copyContent: true);
			return new SparseMatrix<T, TInd>(this.NRows, this.NCols, )
		}

		/// <summary>
		/// Create a new sparse matrix with same properties as this one while the underlying storages are not filled.
		/// </summary>
		/// <returns>The new sparse matrix alike this one</returns>
		public override SparseMatrix<T, TInd> NewArrayAlike();

		/// <summary>
		/// Create a new sparse matrix with same properties as this one while the underlying storages are not filled and the data type is changed to <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged struct as the new data type</typeparam>
		/// <returns>The new sparse matrix alike this one</returns>
		public override SparseMatrix<TOut, TInd> NewArrayAlike<TOut>();
		#endregion
	}
}
