using System;
using System.Collections.Generic;

using Althea.Arrays;
using Althea.Helpers;
using Althea.NativeTypes;
using Althea.LinearAlgebra;

using MEM = Althea.Storage.AbstractApi;
using LAD = Althea.LinearAlgebra.Dense.AbstractApi;
using LAS = Althea.LinearAlgebra.Sparse.AbstractApi;


namespace Althea.Backend.Arrays
{
	/// <summary>
	/// The concrete symmetric dense matrix class with the only <see cref="ValueArray{T}.Storage"/> that refers to the data storage.
	/// </summary>
	/// <typeparam name="T">Any unmanaged struct that implements <see cref="IFormattable"/> and <see cref="IEquatable{T}"/> as the data type</typeparam>
	public class SymmetricDenseMatrix<T> : DenseMatrix<T> where T : unmanaged, IFormattable, IEquatable<T>
	{
		#region basic
		/// <summary>
		/// Get a <see cref="bool"/> indicating whether this symmetric matrix is hermitian or simply symmetric. For real-typed <typeparamref name="T"/>, this is always false.
		/// </summary>
		public bool Hermitian { get; } = false;

		/// <summary>
		/// Get a <see cref="bool"/> indicating whether this symmetric matrix stores the data at upper triangle or lower triangle
		/// </summary>
		public bool StoredUpper { get; } = true;

		/// <summary>
		/// Create an empty <see cref="SymmetricDenseMatrix{T}"/>
		/// </summary>
		public SymmetricDenseMatrix() : base() { }

		/// <summary>
		/// Construct a <see cref="SymmetricDenseMatrix{T}"/> with value array <paramref name="values"/> and size <paramref name="n"/>
		/// </summary>
		/// <param name="values">The value array as a <see cref="Storage{T}"/></param>
		/// <param name="n">The number of rows and columns of this matrix</param>
		/// <param name="leadDim">The leading dimension of this matrix. Default 0 means <paramref name="n"/></param>
		/// <param name="hermitian">Whether this symmetric matrix is hermitian or simply symmetric</param>
		/// <param name="storedUpper">Whether this symmetric matrix stores the data at upper triangle or lower triangle</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="n"/> or <paramref name="leadDim"/> is not positive</exception>
		/// <exception cref="ArgumentException">If <paramref name="leadDim"/> is less than <paramref name="n"/> or the given size exceeds the boundary of <paramref name="values"/></exception>
		public SymmetricDenseMatrix(Storage<T> values, long n, long leadDim = 0, bool hermitian = false, bool storedUpper = true) : base(values, n, n, leadDim)
		{
			this.Hermitian = hermitian && default(T).IsComplex(); this.StoredUpper = storedUpper;
		}
		#endregion

		#region basic indexers


		/// <summary>
		/// Get or set the element at the given position (<paramref name="x"/>, <paramref name="y"/>)
		/// </summary>
		/// <param name="x">The row position as a <see cref="long"/></param>
		/// <param name="y">The column position as a <see cref="long"/></param>
		/// <returns>The element at position (<paramref name="x"/>, <paramref name="y"/>)</returns>
		public override T this[long x, long y] {
			get {
				bool swapped = false;
				if ((this.StoredUpper && x > y) || (!this.StoredUpper && x < y))
				{
					(x, y) = (y, x); swapped = true;
				}
				T value = base[x, y];
				if (this.Hermitian && swapped)
					return value.GenericConjugate();
				else
					return value;
			}
			set {
				if ((this.StoredUpper && x > y) || (!this.StoredUpper && x < y))
				{
					(x, y) = (y, x);
					if (this.Hermitian)
						value = value.GenericConjugate();
				}
				base[x, y] = value;
			}
		}
		#endregion

		#region diagonal indexer
		/// <summary>
		/// Get the <paramref name="k"/>-th diagonal elements.
		/// </summary>
		/// <param name="k">The diagonal index: 0 for diagonal, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <returns>A new <see cref="DenseVector{T}"/> containing the <paramref name="k"/>-th diagonal elements.</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="k"/> is out of range</exception>
		public override DenseVector<T> GetDiag(long k)
		{
			bool swapped = false;
			if ((this.StoredUpper && k < 0) || (!this.StoredUpper && k > 0))
			{
				k = -k; swapped = true;
			}
			var vector = base.GetDiag(k);
			if (this.Hermitian && swapped)
				vector.Conjugate();
			return vector;
		}

		/// <summary>
		/// Get the <paramref name="k"/>-th diagonal elements and write the result to <paramref name="overwrite"/>.
		/// </summary>
		/// <param name="k">The diagonal index: 0 for diagonal, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <param name="overwrite">The output <see cref="VectorBase{T}"/> which will contain the <paramref name="k"/>-th diagonal elements at exit</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="k"/> is out of range</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="overwrite"/> is null or invalid</exception>
		/// <exception cref="ArgumentException">If <paramref name="overwrite"/> cannot be overwritten</exception>
		public override void GetDiag(long k, VectorBase<T> overwrite)
		{
			bool swapped = false;
			if ((this.StoredUpper && k < 0) || (!this.StoredUpper && k > 0))
			{
				k = -k; swapped = true;
			}
			base.GetDiag(k, overwrite);
			if (this.Hermitian && swapped)
				overwrite.Conjugate();
		}

		/// <summary>
		/// Set the <paramref name="k"/>-th diagonal elements to <paramref name="value"/>.
		/// </summary>
		/// <param name="k">The diagonal index: 0 for diagonal, 1 for super-diagonal at one above, -1 for sub-diagonal at one below, etc.</param>
		/// <param name="value">The <paramref name="k"/>-th diagonal elements to set as a <see cref="VectorBase{T}"/></param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="k"/> is out of range</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="value"/> is null or invalid</exception>
		/// <exception cref="NotSupportedException">If <paramref name="value"/> is neither a <see cref="DenseVector{T}"/> nor a <see cref="ISparseVector{T}"/></exception>
		public override void SetDiag(long k, VectorBase<T> value)
		{
			bool conj = false;
			if ((this.StoredUpper && k < 0) || (!this.StoredUpper && k > 0))
			{
				k = -k; conj = this.Hermitian;
			}
			try
			{
				if (conj)
					value.Conjugate();
				base.SetDiag(k, value);
			}
			finally
			{
				if (conj)
					value.Conjugate();
			}
		}
		#endregion

		#region clone related
		/// <summary>
		/// Deep clone the matrix. This implementation utilizes <see cref="Storage{T}.Clone"/>.
		/// </summary>
		/// <returns>The cloned vector</returns>
		public override SymmetricDenseMatrix<T> Clone()
		{
			var c = this.Storage.MakeReference(newLength: this.NRows * this.NCols).CreateAlike();
			try
			{
				MEM.MemoryCopy2D(this.Storage, this.LeadDim, c, this.NRows, this.NRows, this.NCols);
				return new SymmetricDenseMatrix<T>(c, this.NRows, this.NRows, this.Hermitian, this.StoredUpper);
			}
			catch (Exception)
			{
				c?.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Create a new matrix with same properties as this one while the underlying storages are not filled. This implementation utilizes <see cref="Althea.Storage.StorageFactory{T}"/>.
		/// </summary>
		/// <returns>The new matrix alike this one</returns>
		public override SymmetricDenseMatrix<T> NewArrayAlike()
		{
			var c = this.Storage.MakeReference(newLength: this.NRows * this.NCols).CreateAlike();
			return new SymmetricDenseMatrix<T>(c, this.NRows, this.NRows, this.Hermitian, this.StoredUpper);
		}

		/// <summary>
		/// Create a new matrix with same properties as this one while the underlying storages are not filled and the data type is changed to <typeparamref name="TOut"/>. This implementation utilizes <see cref="Althea.Storage.StorageFactory{T}"/> of <typeparamref name="TOut"/>.
		/// </summary>
		/// <typeparam name="TOut">Any unmanaged struct as the new data type</typeparam>
		/// <returns>The new matrix alike this one</returns>
		public override SymmetricDenseMatrix<TOut> NewArrayAlike<TOut>()
		{
			var c = this.Storage.MakeReference(newLength: this.NRows * this.NCols).CreateAlike<TOut>();
			return new SymmetricDenseMatrix<TOut>(c, this.NRows, this.NRows, this.Hermitian, this.StoredUpper);
		}
		#endregion

		#region equality
		/// <summary>
		/// Get the hash code this symmetric dense matrix. The default implementation only takes the hash codes of <see cref="DenseMatrix{T}.GetHashCode"/>, <see cref="Hermitian"/> and <see cref="StoredUpper"/>.
		/// </summary>
		/// <returns>The hash code of <see cref="DenseMatrix{T}.GetHashCode"/>, <see cref="Hermitian"/> and <see cref="StoredUpper"/></returns>
		public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), this.Hermitian, this.StoredUpper);

		/// <summary>
		/// Check whether this object is equal to another one. The default implementation only compares <see cref="ValueArray{T}.Storage"/>.
		/// </summary>
		/// <param name="obj">The other object to compare with</param>
		/// <returns>True if this == <paramref name="obj"/></returns>
		public override bool Equals(object? obj)
		{
			return obj is SymmetricDenseMatrix<T> dm && base.Equals(dm) && this.Hermitian == dm.Hermitian && this.StoredUpper == dm.StoredUpper;
		}
		#endregion

		#region print
		/// <summary>
		/// Print out this symmetric dense matrix.
		/// </summary>
		/// <param name="overrideSetting">Override global settings in <see cref="Settings"/></param>
		/// <returns>The detailed string representation</returns>
		public override string Print(PrintSettings? overrideSetting = null)
		{
			string description = this.ToString();
			if (this.Disposed)
				return description;

			var settings = overrideSetting ?? Settings.PrintSetting;

			string detail = ":" + Environment.NewLine;
			// get managed array
			int n = (int)Math.Min(Math.Min(settings.MatrixRow, settings.MatrixColumn), this.NRows);
			Span<T> managed = (n * n).CheckStackLimit<T>() ?? stackalloc T[n * n];
			MEM.ToManaged2D(this.Storage, this.LeadDim, n, n, managed);
			// copy
			if (this.StoredUpper && this.Hermitian)
			{
				for (int i = 0; i < n; i++)
				{
					for (int j = i + 1; j < n; j++)
					{
						managed[i * n + j] = managed[i + j * n].GenericConjugate();
					}
				}
			}
			else if (this.StoredUpper && !this.Hermitian)
			{
				for (int i = 0; i < n; i++)
				{
					for (int j = i + 1; j < n; j++)
					{
						managed[i * n + j] = managed[i + j * n];
					}
				}
			}
			else if (!this.StoredUpper && this.Hermitian)
			{
				for (int i = 0; i < n; i++)
				{
					for (int j = i + 1; j < n; j++)
					{
						managed[i + j * n] = managed[i * n + j].GenericConjugate();
					}
				}
			}
			else
			{
				for (int i = 0; i < n; i++)
				{
					for (int j = i + 1; j < n; j++)
					{
						managed[i + j * n] = managed[i * n + j];
					}
				}
			}
			// to dense matrix string
			detail += managed.ToMatrixString(n, more: this.NCols - n, precision: settings.Precision);
			if (this.NRows > n)
				detail += Environment.NewLine + string.Format(Resources.Print.MoreRows, this.NRows - n);
			return description + detail;
		}
		#endregion

		#region serialization
		/// <summary>
		/// The print name of the <see cref="Hermitian"/>
		/// </summary>
		protected internal const string HermitianName = nameof(Hermitian);

		/// <summary>
		/// The print name of the <see cref="StoredUpper"/>
		/// </summary>
		protected internal const string StoredUpperName = nameof(StoredUpper);

		/// <summary>
		/// Get other requisite informations for re-constructing the array of that derived class type.
		/// </summary>
		/// <returns>Other requisite informations used to re-construct this array, an empty dictionary.</returns>
		public override IReadOnlyDictionary<string, object> GetMetaData() => new Dictionary<string, object>(3)
		{
			[LeadDimName] = this.LeadDim,
			[HermitianName] = this.Hermitian,
			[StoredUpperName] = this.StoredUpper
		};
		#endregion
	}
}
