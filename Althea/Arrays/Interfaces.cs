using System;
using System.Runtime.CompilerServices;

using Althea.Linq;
using Althea.NativeTypes;
using Althea.Storage;


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
		/// When implemented by a derived class, get (the both-end inclusive accumulated product of <see cref="OuterSize"/>) of this tensor at all dimensions as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>.
		/// </summary>
		/// <remarks>The first element shall be 1, the last element shall be the product of <see cref="OuterSize"/> and the <see cref="ReadOnlySpan{T}.Length">size</see> == rank + 1</remarks>
		ReadOnlySpan<long> Strides { get; }
		#endregion
	}


	/// <summary>
	/// Simple interface for sparse arrays where the index type is not indicated
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	public interface ISparseArray<T> : ICheckValid, IDisposable where T : unmanaged, INumber<T>
	{
		#region property
		/// <summary>
		/// When implemented by a derived class, get the number of non-default values (the values that are actually stored) of this sparse array.
		/// </summary>
		long NStored { get; }

		/// <summary>
		/// When implemented by a derived class, statically get the data type of the <paramref name="n"/>-th index array of this sparse array as a <see cref="DataType"/>.
		/// </summary>
		/// <param name="n">The index of the index array</param>
		/// <returns>The <see cref="DataType"/> of  the <paramref name="n"/>-th index array.</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="n"/> is out of range</exception>
		abstract static DataType IndexType(int n);

		/// <summary>
		/// When implemented by a derived class, statically get the default value of this sparse array
		/// </summary>
		abstract static T DefaultValue { get; }
		#endregion

		#region dispose
		/// <summary>
		/// When implemented by a derived class, get the original value array's storage of this sparse array. This property is only used for disposition.
		/// </summary>
		protected IStorage ValueStorage { get; }

		/// <summary>
		/// When implemented by a derived class, get the original index array(s)' storage(s) of this sparse array. This property is only used for disposition.
		/// </summary>
		protected ReadOnlySpan<IStorage> IndexStorages { get; }

		/// <summary>
		/// When implemented by a derived class, actually dispose this sparse array's index storages. The default implementation disposes <see cref="ISparseArray{T}.IndexStorages"/>.
		/// </summary>
		void IDisposable.Dispose()
		{
			this.ValueStorage?.Dispose();
			var list = this.IndexStorages;
			for (int i = 0; i < list.Length; i++)
			{
				list[i]?.Dispose();
			}
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// When implemented by a derived class, dispose this sparse array's index storages after excluding the internal ones shared between this array and the target <paramref name="array"/>. The default implementation only compares the two <see cref="ISparseArray{T}.IndexStorages"/>.
		/// </summary>
		/// <param name="array">The target <see cref="ISparseArray{T}"/> to exclude before disposing</param>
		void DisposeExclude(ISparseArray<T> array)
		{
			var list = this.IndexStorages;
			var other = array.IndexStorages;
			for (int i = 0; i < list.Length; i++)
			{
				bool canDispose = true;
				for (int j = 0; j < other.Length; j++)
				{
					if (list[i].OverlapWith(other[j]))
					{
						canDispose = false;
						break;
					}
				}
				if (canDispose)
					list[i].Dispose();
			}
		}

		bool ICheckValid.IsValid()
		{
			if (this.NStored <= 0)
				return false;
			var list = this.IndexStorages;
			return list.All(static l => l is not null && l.IsValid());
		}
		#endregion
	}

	/// <summary>
	/// The interface for tensor that contains basic members (size and labels).
	/// </summary>
	public interface ILabeledTensor
	{
		#region properties
		/// <summary>
		/// When implemented by a derived class, get the rank of this tensor.
		/// </summary>
		int Rank { get; }

		/// <summary>
		/// When implemented by a derived class, get the size of this array (the extent at all dimensions) as a <see cref="ReadOnlySpan{T}"/> of <see cref="long"/>, must be of positive numbers.
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
}
