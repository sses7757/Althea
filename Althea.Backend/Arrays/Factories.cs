using System;
using System.Collections.Generic;

using Althea.Arrays;
using Althea.Linq;
using Althea.LinearAlgebra.Sparse;


namespace Althea.Backend.Arrays
{
	internal sealed class DenseVectorFactory : IArrayFactory
	{
		public ValueArray<T> CreateArray<T>(ReadOnlySpan<long> size, IReadOnlyDictionary<string, IStorage> storages, IReadOnlyDictionary<string, object>? otherInfo = null) where T : unmanaged
		{
			if (size.Length != 1 || size[0] <= 0)
				throw new ArgumentOutOfRangeException(nameof(size));
			if (storages is null || storages.Count != 1)
				throw new ArgumentNullException(nameof(storages));
			if (otherInfo is not null && otherInfo.Count != 0)
				throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(otherInfo));
			var values = ValueArrayFactory<T>.CheckValueStorage(storages, size[0]);

			return new DenseVector<T>(values);
		}
	}

	internal sealed class SparseVectorFactory<TInd> : IArrayFactory where TInd : unmanaged
	{
		public ValueArray<T> CreateArray<T>(ReadOnlySpan<long> size, IReadOnlyDictionary<string, IStorage> storages, IReadOnlyDictionary<string, object>? otherInfo = null) where T : unmanaged
		{
			if (size.Length != 1 || size[0] <= 0)
				throw new ArgumentOutOfRangeException(nameof(size));
			if (storages is null || storages.Count != 2)
				throw new ArgumentNullException(nameof(storages));
			if (otherInfo is null || otherInfo.Count != 1)
				throw new ArgumentNullException(nameof(otherInfo));

			var values = ValueArrayFactory<T>.CheckValueStorage(storages);
			var indices = ValueArrayFactory<T>.CheckStorage<TInd>(storages, SparseVector<T, TInd>.IndexStorageName);
			var defaultValue = (T)(dynamic)otherInfo[SparseVector<T, TInd>.DefaultValueName];

			return new SparseVector<T, TInd>(size[0], values, indices, defaultValue);
		}
	}

	internal sealed class DenseMatrixFactory : IArrayFactory
	{
		public ValueArray<T> CreateArray<T>(ReadOnlySpan<long> size, IReadOnlyDictionary<string, IStorage> storages, IReadOnlyDictionary<string, object>? otherInfo = null) where T : unmanaged
		{
			if (size.Length != 2 || size[0] <= 0 || size[1] <= 0)
				throw new ArgumentOutOfRangeException(nameof(size));
			if (storages is null || storages.Count != 1)
				throw new ArgumentNullException(nameof(storages));
			if (otherInfo is null || otherInfo.Count != 1)
				throw new ArgumentNullException(nameof(otherInfo));

			var leadDim = (long)(dynamic)otherInfo[DenseMatrix<T>.LeadDimName];
			var values = ValueArrayFactory<T>.CheckValueStorage(storages, leadDim * (size[1] - 1) + size[0]);

			return new DenseMatrix<T>(values, size[0], size[1], leadDim);
		}
	}

	internal sealed class SymmetricDenseMatrixFactory : IArrayFactory
	{
		public ValueArray<T> CreateArray<T>(ReadOnlySpan<long> size, IReadOnlyDictionary<string, IStorage> storages, IReadOnlyDictionary<string, object>? otherInfo = null) where T : unmanaged
		{
			if (size.Length != 2 || size[0] <= 0 || size[1] != size[0])
				throw new ArgumentOutOfRangeException(nameof(size));
			if (storages is null || storages.Count != 1)
				throw new ArgumentNullException(nameof(storages));
			if (otherInfo is null || otherInfo.Count != 3)
				throw new ArgumentNullException(nameof(otherInfo));

			var leadDim = (long)(dynamic)otherInfo[SymmetricDenseMatrix<T>.LeadDimName];
			var values = ValueArrayFactory<T>.CheckValueStorage(storages, leadDim * (size[1] - 1) + size[0]);
			var hermitian = (bool)otherInfo[SymmetricDenseMatrix<T>.HermitianName];
			var upper = (bool)otherInfo[SymmetricDenseMatrix<T>.StoredUpperName];

			return new SymmetricDenseMatrix<T>(values, size[0], leadDim, hermitian, upper);
		}
	}

	internal sealed class SparseMatrixFactory<TInd> : IArrayFactory where TInd : unmanaged, IEquatable<TInd>
	{
		public ValueArray<T> CreateArray<T>(ReadOnlySpan<long> size, IReadOnlyDictionary<string, IStorage> storages, IReadOnlyDictionary<string, object>? otherInfo = null) where T : unmanaged
		{
			if (size.Length != 1 || size[0] <= 0 || size[1] <= 0)
				throw new ArgumentOutOfRangeException(nameof(size));
			if (storages is null || storages.Count != 3)
				throw new ArgumentNullException(nameof(storages));
			if (otherInfo is null || otherInfo.Count != 2)
				throw new ArgumentNullException(nameof(otherInfo));

			var values = ValueArrayFactory<T>.CheckValueStorage(storages);
			var row = ValueArrayFactory<T>.CheckStorage<TInd>(storages, SparseMatrix<T, TInd>.RowIndexStorageName);
			var column = ValueArrayFactory<T>.CheckStorage<TInd>(storages, SparseMatrix<T, TInd>.ColIndexStorageName);
			var defaultValue = (T)(dynamic)otherInfo[SparseMatrix<T, TInd>.DefaultValueName];
			var format_ = otherInfo[SparseMatrix<T, TInd>.FormatName];
			SparseMatrixFormat format;
			if (format_ is string s)
			{
				if (!Enum.TryParse(s, ignoreCase: false, out format))
					if (!Enum.TryParse(s, ignoreCase: true, out format))
						throw new ArgumentException(Resources.Other.CannotParse, nameof(otherInfo));
			}
			else if (format_ is int i)
			{
				format = (SparseMatrixFormat)i;
			}
			else
				throw new ArgumentException(Resources.Other.CannotParse, nameof(otherInfo));

			return new SparseMatrix<T, TInd>(size[0], size[1], values, row, column, format, defaultValue);
		}
	}

	internal sealed class BlockedSparseMatrixFactory<TInd> : IArrayFactory where TInd : unmanaged, IEquatable<TInd>
	{
		public ValueArray<T> CreateArray<T>(ReadOnlySpan<long> size, IReadOnlyDictionary<string, IStorage> storages, IReadOnlyDictionary<string, object>? otherInfo = null) where T : unmanaged
		{
			if (size.Length != 1 || size[0] <= 0 || size[1] <= 0)
				throw new ArgumentOutOfRangeException(nameof(size));
			if (storages is null || storages.Count != 3)
				throw new ArgumentNullException(nameof(storages));
			if (otherInfo is null || otherInfo.Count != 2)
				throw new ArgumentNullException(nameof(otherInfo));

			var values = ValueArrayFactory<T>.CheckValueStorage(storages);
			var row = ValueArrayFactory<T>.CheckStorage<TInd>(storages, BlockedSparseMatrix<T, TInd>.RowIndexStorageName);
			var column = ValueArrayFactory<T>.CheckStorage<TInd>(storages, BlockedSparseMatrix<T, TInd>.ColIndexStorageName);
			var blockNRows = (long)(dynamic)otherInfo[BlockedSparseMatrix<T, TInd>.BlockNRowsName];
			var blockNCols = (long)(dynamic)otherInfo[BlockedSparseMatrix<T, TInd>.BlockNColsName];
			var defaultValue = (T)(dynamic)otherInfo[BlockedSparseMatrix<T, TInd>.DefaultValueName];
			var format_ = otherInfo[BlockedSparseMatrix<T, TInd>.FormatName];
			SparseMatrixFormat format;
			if (format_ is string s)
			{
				if (!Enum.TryParse(s, ignoreCase: false, out format))
					if (!Enum.TryParse(s, ignoreCase: true, out format))
						throw new ArgumentException(Resources.Other.CannotParse, nameof(otherInfo));
			}
			else if (format_ is int i)
			{
				format = (SparseMatrixFormat)i;
			}
			else
				throw new ArgumentException(Resources.Other.CannotParse, nameof(otherInfo));

			return new BlockedSparseMatrix<T, TInd>(blockNRows, blockNCols, size[0], size[1], values, row, column, format, defaultValue);
		}
	}

	internal sealed class DenseTensorFactory : IArrayFactory
	{
		public ValueArray<T> CreateArray<T>(ReadOnlySpan<long> size, IReadOnlyDictionary<string, IStorage> storages, IReadOnlyDictionary<string, object>? otherInfo = null) where T : unmanaged
		{
			if (size.Any(static s => s <= 0))
				throw new ArgumentOutOfRangeException(nameof(size));
			if (storages is null || storages.Count != 1)
				throw new ArgumentNullException(nameof(storages));
			if (otherInfo is null || otherInfo.Count != 2)
				throw new ArgumentNullException(nameof(otherInfo));

			// get outer size
			long[] outerSize;
			var outer = otherInfo[DenseTensor<T>.OuterSizeName];
			if (outer is long[] outerLong)
				outerSize = outerLong;
			else if (outer is int[] outerInt)
				outerSize = Array.ConvertAll(outerInt, static i => (long)i);
			else
				throw new ArgumentException(string.Format(Resources.Other.CannotParse, outer.GetType(), typeof(long[])), nameof(otherInfo));
			// get labels
			char[] labels;
			var label = otherInfo[DenseTensor<T>.LabelsName];
			if (label is char[] c)
				labels = c;
			else if (label is string s)
				labels = s.ToCharArray();
			else if (label is int[] ii)
				labels = Array.ConvertAll(ii, static i => (char)i);
			else
				throw new ArgumentException(string.Format(Resources.Other.CannotParse, outer.GetType(), typeof(char[])), nameof(otherInfo));
			// get values
			var values = ValueArrayFactory<T>.CheckValueStorage(storages, DenseTensor<T>.GetActualLength(size, outerSize));
			// return
			return new DenseTensor<T>(values, size, outerSize, labels);
		}
	}
}
