using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Althea.LinearAlgebra.Sparse;
using Althea.Linq;


namespace Althea.Backend.Arrays
{
	internal static class Helpers
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static SparseVector<T, TInd> CheckWrapper<T, TInd>(this SparseArrayWrapper<T> vector, long length) where T : unmanaged where TInd : unmanaged
		{
			if (vector.ValueStorage is null || vector.ValueStorage.Length <= 0)
				throw new ArgumentOutOfRangeException(nameof(vector), vector.ValueStorage?.Length, Resources.Parameter.ZeroSize);
			if (vector.IndexStorages.Length == 0)
				throw new ArgumentOutOfRangeException(nameof(vector), vector.IndexStorages.Length, Resources.Parameter.ZeroSize);
			if (vector.IndexStorages.Length != 1)
				throw new ArgumentOutOfRangeException(nameof(vector), vector.IndexStorages.Length, Resources.Parameter.WrongSize);
			if (vector.IndexStorages[0] is not Storage<TInd> indices)
				throw new ArgumentOutOfRangeException(nameof(vector), vector.IndexStorages[0], Resources.Parameter.UnexpectedType);
			if (indices.Length != vector.ValueStorage.Length)
				throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(vector));
			if (vector.ValueStorage.Length > length)
				throw new ArgumentOutOfRangeException(nameof(length), length, Resources.Parameter.InvalidValue);
			if (vector.VectorFormat != SparseVectorFormat.Coordinated)
				throw new ArgumentOutOfRangeException(nameof(vector), vector.VectorFormat, Resources.Parameter.InvalidValue);

			return new SparseVector<T, TInd>(length, vector.ValueStorage, indices, vector.DefaultValue);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Althea.Arrays.BaseSparseMatrix<T, TInd> CheckWrapper<T, TInd>(this SparseArrayWrapper<T> matrix, long rows, long cols) where T : unmanaged where TInd : unmanaged
		{
			if (matrix.ValueStorage is null || matrix.ValueStorage.Length <= 0)
				throw new ArgumentOutOfRangeException(nameof(matrix), matrix.ValueStorage?.Length, Resources.Parameter.ZeroSize);
			if (matrix.IndexStorages.Length == 0)
				throw new ArgumentOutOfRangeException(nameof(matrix), matrix.IndexStorages.Length, Resources.Parameter.ZeroSize);
			if (matrix.IndexStorages.Length != 2)
				throw new ArgumentOutOfRangeException(nameof(matrix), matrix.IndexStorages.Length, Resources.Parameter.WrongSize);
			if (matrix.IndexStorages[0] is not Storage<TInd> rowIndex)
				throw new ArgumentOutOfRangeException(nameof(matrix), matrix.IndexStorages[0], Resources.Parameter.UnexpectedType);
			if (matrix.IndexStorages[1] is not Storage<TInd> colIndex)
				throw new ArgumentOutOfRangeException(nameof(matrix), matrix.IndexStorages[1], Resources.Parameter.UnexpectedType);
			if (matrix.ValueStorage.Length > rows * cols)
				throw new ArgumentException(Resources.Parameter.WrongSize);

			if ((matrix.MatrixFormat & FormatExtension.NonBlocked) == matrix.MatrixFormat)
				return new SparseMatrix<T, TInd>(rows, cols, matrix.ValueStorage, rowIndex, colIndex, matrix.MatrixFormat, matrix.DefaultValue);
			else if ((matrix.MatrixFormat & FormatExtension.Blocked) == matrix.MatrixFormat)
			{
				if (matrix.OtherInfo is not BlockedSparseMatrixOtherInfo info)
					throw new ArgumentOutOfRangeException(nameof(matrix), matrix.OtherInfo, Resources.Parameter.UnexpectedType);
				return new BlockedSparseMatrix<T, TInd>(rows, cols, info.BlockRows, info.BlockCols, matrix.ValueStorage, rowIndex, colIndex, matrix.MatrixFormat, matrix.DefaultValue);
			}
			else
				throw new ArgumentOutOfRangeException(nameof(matrix), matrix.VectorFormat, Resources.Parameter.InvalidValue);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Althea.Arrays.BaseSparseTensor<T, TInd> CheckWrapper<T, TInd>(this SparseArrayWrapper<T> tensor, ReadOnlySpan<long> size, ReadOnlySpan<char> labels) where T : unmanaged where TInd : unmanaged
		{
			if (tensor.ValueStorage is null || tensor.ValueStorage.Length <= 0)
				throw new ArgumentOutOfRangeException(nameof(tensor), tensor.ValueStorage?.Length, Resources.Parameter.ZeroSize);
			if (tensor.IndexStorages.Length == 0)
				throw new ArgumentOutOfRangeException(nameof(tensor), tensor.IndexStorages.Length, Resources.Parameter.ZeroSize);
			if (tensor.ValueStorage.Length > size.Prod())
				throw new ArgumentOutOfRangeException(nameof(size), size.Prod(), Resources.Parameter.InvalidValue);

			if (tensor.TensorFormat == TensorAlgebra.Sparse.SparseTensorFormat.Coordinated)
			{
				if (tensor.IndexStorages.Length != 1)
					throw new ArgumentOutOfRangeException(nameof(tensor), tensor.IndexStorages.Length, Resources.Parameter.WrongSize);
				if (tensor.IndexStorages[0] is not Storage<TInd> indices)
					throw new ArgumentOutOfRangeException(nameof(tensor), tensor.IndexStorages[0], Resources.Parameter.UnexpectedType);
				if (indices.Length != tensor.ValueStorage.Length)
					throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(tensor));

				return new SparseTensor<T, TInd>(size, tensor.ValueStorage, indices, labels, defaultValue: tensor.DefaultValue);
			}
			else if (tensor.TensorFormat == TensorAlgebra.Sparse.SparseTensorFormat.BlockCoordinated)
			{
				if (tensor.OtherInfo is not BlockedSparseTensorOtherInfo info)
					throw new ArgumentOutOfRangeException(nameof(tensor), tensor.OtherInfo, Resources.Parameter.InvalidValue);
				if (tensor.IndexStorages.Length != size.Length)
					throw new ArgumentOutOfRangeException(nameof(tensor), tensor.IndexStorages.Length, Resources.Parameter.WrongSize);
				if (tensor.IndexStorages.Any(static s => s is not Storage<TInd>))
					throw new ArgumentOutOfRangeException(nameof(tensor), tensor.IndexStorages.ToArray(), Resources.Parameter.UnexpectedType);
				var values = tensor.ValueStorage;
				if (tensor.IndexStorages.Any(s => s.Length != values.Length))
					throw new ArgumentException(Resources.Parameter.NotSameSize, nameof(tensor));

				var span = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<IStorage, Storage<TInd>>(ref tensor.IndexStorages.Ref()), size.Length);
				return new BlockedSparseTensor<T, TInd>(size, info.BlockSize, tensor.ValueStorage, span, labels, defaultValue: tensor.DefaultValue);
			}
			else if (tensor.TensorFormat == TensorAlgebra.Sparse.SparseTensorFormat.VariableBlockCoordinated)
			{
				// TODO: variable blocked sparse tensor
				throw new NotImplementedException();
			}
			else
				throw new ArgumentOutOfRangeException(nameof(tensor), tensor.TensorFormat, Resources.Parameter.InvalidValue);
		}
	}
}
