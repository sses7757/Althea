using System;

using Althea.Helpers;
using Althea.LinearAlgebra.Sparse;
using Althea.Linq;


namespace Althea.Arrays
{
	/// <summary>
	/// The interface of (column-major) dense array that may exist extra pitch at each dimension and thus the strides are not simply the accumulated product of its size.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	public interface IPitchedArray<T> where T : unmanaged, INumber<T>
	{
		#region properties
		/// <summary>
		/// When implemented by a derived class, get the size (in <typeparamref name="T"/>) of this array (the extent at each dimension) as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>, must be of positive numbers.
		/// </summary>
		ReadOnlySpan<long> Size { get; }

		/// <summary>
		/// When implemented by a derived class, get the pitch (in <typeparamref name="T"/>) of this array (the outer size at each dimension) as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>. It must has length equals to <see cref="Size"/> and consists numbers larger than or equals to <see cref="Size"/> respectively.
		/// </summary>
		ReadOnlySpan<long> OuterSize { get; }

		/// <summary>
		/// When implemented by a derived class, check whether this array is actually pitched. The default implementation simply checks the point-wise equality of <see cref="Size"/> and <see cref="OuterSize"/>.
		/// </summary>
		bool HasPitch => !this.OuterSize.SequenceEqual(this.Size);

		/// <summary>
		/// When implemented by a derived class, get (the exclusive accumulated product of <see cref="OuterSize"/>) of this tensor at all dimensions as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>.
		/// </summary>
		/// <remarks>The first element shall be 1 and the last element shall be the product of <see cref="OuterSize"/>. The returned <see cref="ReadOnlySpan{T}.Length">size</see> == rank + 1</remarks>
		protected ReadOnlySpan<long> Strides { get; }
		#endregion
	}

	/// <summary>
	/// The base interface for sparse arrays.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	public interface ISparseArray<T> where T : unmanaged, INumber<T>
	{
		#region properties
		/// <summary>
		/// When implemented by a derived class, get the sparse format of this sparse array as a <see cref="LinearAlgebra.Sparse.SparseFormat"/>.
		/// </summary>
		LinearAlgebra.Sparse.SparseFormat Format { get; }

		/// <summary>
		/// When implemented by a derived class, get the default value of this sparse array
		/// </summary>
		T DefaultValue { get; }

		/// <summary>
		/// When implemented by a derived class, get number of elements actually stored this sparse vector.
		/// </summary>
		long NStored { get; }

		/// <summary>
		/// When implemented by a derived class, get the size of this array (the extent at all dimensions) in <typeparamref name="T"/> as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>, must be of positive numbers.
		/// </summary>
		ReadOnlySpan<long> Size { get; }
		#endregion
	}

	/// <summary>
	/// The complicated interface for sparse arrays.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the value data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value array(s)</typeparam>
	/// <typeparam name="TInd">Any unmanaged integer number as the index data type</typeparam>
	/// <typeparam name="TSInd">The storage type used by the index array(s)</typeparam>
	public interface ISparseArray<T, TInd, TS, TSInd> : ISparseArray<T>
		where T : unmanaged, INumber<T> where TInd : unmanaged, IBinaryInteger<TInd>
		where TS : class, Storage.IStorage<T, TS> where TSInd : class, Storage.IStorage<TInd, TSInd>
	{
		#region properties
		/// <summary>
		/// When implemented by a derived class, get the value array(s) of this sparse array
		/// </summary>
		ReadOnlySpan<TS> ValueStorages { get; }

		/// <summary>
		/// When implemented by a derived class, get the index array(s) of this sparse array
		/// </summary>
		ReadOnlySpan<TSInd> IndexStorages { get; }

		/// <summary>
		/// When implemented by a derived class, get the constant block size of this sparse array, can be empty if it is not a <see cref="SparseFormat.Blocking.Simple"/>
		/// </summary>
		ReadOnlySpan<TInd> BlockSize { get; }
		#endregion
	}

	/// <summary>
	/// The interface for tensor that contains basic members (size and labels).
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	public interface ILabeledTensor<T> where T : unmanaged, INumber<T>
	{
		#region properties
		/// <summary>
		/// When implemented by a derived class, get the rank of this tensor.
		/// </summary>
		int Rank { get; }

		/// <summary>
		/// When implemented by a derived class, get the size of this array (the extent at all dimensions) in <typeparamref name="T"/> as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>, must be of positive numbers.
		/// </summary>
		ReadOnlySpan<long> Size { get; }

		/// <summary>
		/// When implemented by a derived class, get or set the label array as a <see cref="ReadOnlySpan{T}"/> of <see cref="char"/> used to mark each index of this tensor.
		/// </summary>
		/// <exception cref="ArgumentException">If the setting value's length is not the same as the <see cref="Rank"/></exception>
		ReadOnlySpan<char> Labels { get; set; }
		#endregion

		#region method
		/// <summary>
		/// When implemented by a derived class, get the label at rank <paramref name="index"/>
		/// </summary>
		/// <param name="index">The index of the rank whose label will be obtained</param>
		/// <returns>The <see cref="char"/> label at <paramref name="index"/></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range of <see cref="Rank"/></exception>
		char GetLabel(int index);

		/// <summary>
		/// When implemented by a derived class, set the label at rank <paramref name="index"/> to <paramref name="value"/>
		/// </summary>
		/// <param name="index">The index of the rank whose label will be set</param>
		/// <param name="value">The <see cref="char"/> label at <paramref name="index"/> to set</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="index"/> is out of range of <see cref="Rank"/></exception>
		void SetLabel(int index, char value);

		/// <summary>
		/// When implemented by a derived class, set the label(s) used to mark each index of this tensor
		/// </summary>
		/// <param name="labels">The label(s) to set as an array of <see cref="char"/></param>
		/// <exception cref="ArgumentNullException">If <paramref name="labels"/> is null or empty</exception>
		/// <exception cref="ArgumentException">If the length of <paramref name="labels"/> is not the same as the <see cref="Rank"/></exception>
		void SetLabels(params char[] labels);
		#endregion
	}

	/// <summary>
	/// The interface for printable arrays.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	public interface IPrintable<T> where T : unmanaged, INumber<T>
	{
		/// <summary>
		/// When implemented by a derived class, print this array to <see cref="string"/> under given print <paramref name="settings"/>.
		/// </summary>
		/// <param name="settings">The <see cref="PrintSettings"/> to use, default null means <see cref="Settings.PrintSetting"/></param>
		/// <returns>The printed array as a <see cref="string"/>.</returns>
		string Print(PrintSettings? settings = null);
	}
}
