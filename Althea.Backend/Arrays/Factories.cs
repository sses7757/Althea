using System;
using System.Collections.Generic;

using Althea.Linq;
using Althea.Arrays;


namespace Althea.Backend.Arrays
{
	internal sealed class DenseVectorFactory : IArrayFactory
	{
		public ValueArray<T> CreateArray<T>(ReadOnlySpan<long> size, IReadOnlyDictionary<string, IStorage> storages, IReadOnlyDictionary<string, object>? otherInfo = null) where T : unmanaged, IFormattable, IEquatable<T>
		{
			if (size.Length != 1 || size[0] <= 0)
				throw new ArgumentOutOfRangeException(nameof(size));
			if (storages is null || storages.Count < 1)
				throw new ArgumentNullException(nameof(storages));
			if (otherInfo is not null && otherInfo.Count != 0)
				throw new ArgumentException(Resources.Parameter.InvalidValue, nameof(otherInfo));
			var values = ValueArrayFactory<T>.CheckValueStorage(storages, size[0]);

			return new DenseVector<T>(values);
		}
	}

	internal sealed class SparseVectorFactory : IArrayFactory
	{
		public ValueArray<T> CreateArray<T>(ReadOnlySpan<long> size, IReadOnlyDictionary<string, IStorage> storages, IReadOnlyDictionary<string, object>? otherInfo = null) where T : unmanaged, IFormattable, IEquatable<T>
		{
			if (size.Length != 1 || size[0] <= 0)
				throw new ArgumentOutOfRangeException(nameof(size));
			if (storages is null || storages.Count < 2)
				throw new ArgumentNullException(nameof(storages));
			if (otherInfo is null || otherInfo.Count != 2)
				throw new ArgumentNullException(nameof(otherInfo));
			var values = ValueArrayFactory<T>.CheckValueStorage(storages);
			var indices = ValueArrayFactory<T>.CheckStorage(storages);

			return new SparseVector<T, int>();
		}

		private static long Check(IReadOnlyList<long> size, IReadOnlyDictionary<string, object> otherInfo)
		{
			if (size is null || size.Count != 1)
				throw new ArgumentNullException(nameof(size));
			if (otherInfo is null || !otherInfo.ContainsKey(NonZerosName))
				throw new ArgumentNullException(nameof(otherInfo));
			var type = otherInfo[NonZerosName].GetType();
			long nnz = type == typeof(long) ? (long)otherInfo[NonZerosName] :
						type == typeof(int) ? (int)otherInfo[NonZerosName] :
							throw new ArgumentNullException(nameof(otherInfo));
			return nnz;
		}

		private static (Storage<T> value, Storage<int> index) Check<T>(IReadOnlyDictionary<string, IStorage> pointers) where T : unmanaged, IFormattable, IEquatable<T>
		{
			var value = ValueArrayFactory.CheckPointer<T>(pointers, ValueArray<T>.StorageName);
			var index = ValueArrayFactory.CheckPointer<int>(pointers, IndexPointerName);
			return (value, index);
		}

		public ValueArray<T> CreateArray<T>(IReadOnlyList<long> size, bool onHost, IReadOnlyDictionary<string, object> otherInfo = null) where T : unmanaged, IFormattable, IEquatable<T>
		{
			long nnz = Check(size, otherInfo);
			return new SparseVector<T>(size[0], nonZeros: nnz, onHost);
		}

		public ValueArray<T> ReconstructArray<T>(IReadOnlyList<long> size, IReadOnlyDictionary<string, IStorage> pointers, IReadOnlyDictionary<string, object> otherInfo = null) where T : unmanaged, IFormattable, IEquatable<T>
		{
			var (value, index) = Check<T>(pointers);
			return new SparseVector<T>(size[0], value, index);
		}

		internal static IReadOnlyDictionary<string, IStorage> GetPointers<T>(SparseVector<T> vec) where T : unmanaged, IFormattable, IEquatable<T>
		{
			return new Dictionary<string, IStorage>
			{
				[ValueArray<T>.StorageName] = vec.Storage,
				[IndexPointerName] = vec.IndexPointer
			};
		}

		internal static IReadOnlyDictionary<string, object> GetOtherInfo<T>(SparseVector<T> vec) where T : unmanaged, IFormattable, IEquatable<T>
		{
			return new Dictionary<string, object>
			{
				[NonZerosName] = vec.NonZero
			};
		}
	}

	internal sealed class DenseMatrixFactory : IArrayFactory
	{
		// Ignore Spelling: ld herm
		private const string HermitianName = nameof(DenseMatrix<int>.Hermitian);
		private const string LeadDimensionName = nameof(DenseMatrix<int>.LeadDim);

		private static bool Check(IReadOnlyList<long> size, IReadOnlyDictionary<string, object> otherInfo)
		{
			if (size is null || size.Count != 2)
				throw new ArgumentNullException(nameof(size));
			if (otherInfo is null || !otherInfo.ContainsKey(HermitianName))
				return false;
			var type = otherInfo[HermitianName].GetType();
			return type == typeof(bool) ? (bool)otherInfo[HermitianName] : throw new ArgumentNullException(nameof(otherInfo));
		}

		private static (long ld, bool herm, Storage<T> data) Check<T>(IReadOnlyList<long> size, IReadOnlyDictionary<string, IStorage> pointers, IReadOnlyDictionary<string, object> otherInfo) where T : unmanaged, IFormattable, IEquatable<T>
		{
			if (size is null || size.Count != 2)
				throw new ArgumentNullException(nameof(size));

			bool herm;
			if (otherInfo is null || !otherInfo.ContainsKey(HermitianName))
				herm = false;
			else
			{
				var type = otherInfo[HermitianName].GetType();
				herm = type == typeof(bool) ? (bool)otherInfo[HermitianName] : throw new ArgumentNullException(nameof(otherInfo));
			}
			if (herm && size[0] != size[1])
				throw new ArgumentException(Resource.MatMustSquare, nameof(size));

			long ld;
			if (otherInfo is null || !otherInfo.ContainsKey(LeadDimensionName))
				ld = size[0];
			else
			{
				var type = otherInfo[LeadDimensionName].GetType();
				ld = type == typeof(long) ? (long)otherInfo[LeadDimensionName] :
						type == typeof(int) ? (int)otherInfo[LeadDimensionName] :
								throw new ArgumentNullException(nameof(otherInfo));
			}

			var data = ValueArrayFactory.CheckPointer<T>(pointers, ValueArray<T>.StorageName, ld * size[1]);
			return (ld, herm, data);
		}

		public ValueArray<T> CreateArray<T>(IReadOnlyList<long> size, bool onHost, IReadOnlyDictionary<string, object> otherInfo = null) where T : unmanaged, IFormattable, IEquatable<T>
		{
			bool herm = Check(size, otherInfo);
			return new DenseMatrix<T>(size[0], size[1], onHost, herm);
		}

		public ValueArray<T> ReconstructArray<T>(IReadOnlyList<long> size, IReadOnlyDictionary<string, IStorage> pointers, IReadOnlyDictionary<string, object> otherInfo = null) where T : unmanaged, IFormattable, IEquatable<T>
		{
			var (ld, herm, data) = Check<T>(size, pointers, otherInfo);
			return new DenseMatrix<T>(data, size[0], size[1], ld, herm);
		}

		internal static IReadOnlyDictionary<string, IStorage> GetPointers<T>(DenseMatrix<T> mat) where T : unmanaged, IFormattable, IEquatable<T>
		{
			return new Dictionary<string, IStorage>
			{
				[ValueArray<T>.StorageName] = mat.Storage
			};
		}

		internal static IReadOnlyDictionary<string, object> GetOtherInfo<T>(DenseMatrix<T> mat) where T : unmanaged, IFormattable, IEquatable<T>
		{
			return new Dictionary<string, object>
			{
				[HermitianName] = mat.Hermitian,
				[LeadDimensionName] = mat.LeadDim
			};
		}
	}

	internal sealed class SparseMatrixFactory : IArrayFactory
	{
		private const string RowIndexName = nameof(SparseMatrix<int>.RowPointer);
		private const string ColumnIndexName = nameof(SparseMatrix<int>.ColumnPointer);

		private const string NonZerosName = nameof(SparseMatrix<int>.NonZero);
		private const string HermitianName = nameof(SparseMatrix<int>.Hermitian);
		private const string FormatName = nameof(SparseMatrix<int>.Format);

		private static (long nnz, SparseMatrixFormat format, bool herm) Check(IReadOnlyList<long> size, IReadOnlyDictionary<string, object> otherInfo)
		{
			if (size is null || size.Count != 2)
				throw new ArgumentNullException(nameof(size));

			bool herm;
			if (otherInfo is null || !otherInfo.ContainsKey(HermitianName))
				herm = false;
			else
			{
				var type = otherInfo[HermitianName].GetType();
				herm = type == typeof(bool) ? (bool)otherInfo[HermitianName] : throw new ArgumentNullException(nameof(otherInfo));
			}

			SparseMatrixFormat format;
			if (otherInfo is null || !otherInfo.ContainsKey(FormatName))
				throw new ArgumentNullException(nameof(otherInfo));
			else
			{
				var type = otherInfo[FormatName].GetType();
				format = type == typeof(SparseMatrixFormat) ? (SparseMatrixFormat)otherInfo[FormatName] : throw new ArgumentNullException(nameof(otherInfo));
			}

			long nnz;
			if (otherInfo is null || !otherInfo.ContainsKey(NonZerosName))
				throw new ArgumentNullException(nameof(otherInfo));
			else
			{
				var type = otherInfo[NonZerosName].GetType();
				nnz = type == typeof(long) ? (long)otherInfo[NonZerosName] :
						type == typeof(int) ? (int)otherInfo[NonZerosName] :
								throw new ArgumentNullException(nameof(otherInfo));
			}

			return (nnz, format, herm);
		}

		private static (SparseMatrixFormat format, bool herm, Storage<T> value, Storage<int> row, Storage<int> col) Check<T>(IReadOnlyList<long> size, IReadOnlyDictionary<string, IStorage> pointers, IReadOnlyDictionary<string, object> otherInfo) where T : unmanaged, IFormattable, IEquatable<T>
		{
			if (size is null || size.Count != 2)
				throw new ArgumentNullException(nameof(size));

			bool herm;
			if (otherInfo is null || !otherInfo.ContainsKey(HermitianName))
				herm = false;
			else
			{
				var type = otherInfo[HermitianName].GetType();
				herm = type == typeof(bool) ? (bool)otherInfo[HermitianName] : throw new ArgumentNullException(nameof(otherInfo));
			}
			if (herm && size[0] != size[1])
				throw new ArgumentException(Resource.MatMustSquare, nameof(size));

			SparseMatrixFormat format;
			if (otherInfo is null || !otherInfo.ContainsKey(FormatName))
				format = SparseMatrixFormat.Any;
			else
			{
				var type = otherInfo[FormatName].GetType();
				format = type == typeof(SparseMatrixFormat) ? (SparseMatrixFormat)otherInfo[FormatName] : throw new ArgumentNullException(nameof(otherInfo));
			}

			long nnz;
			if (otherInfo is null || !otherInfo.ContainsKey(NonZerosName))
				nnz = 0;
			else
			{
				var type = otherInfo[NonZerosName].GetType();
				nnz = type == typeof(long) ? (long)otherInfo[NonZerosName] :
						type == typeof(int) ? (int)otherInfo[NonZerosName] :
								throw new ArgumentNullException(nameof(otherInfo));
			}

			if (format == SparseMatrixFormat.Any && nnz == 0)
				throw new ArgumentNullException(nameof(otherInfo));

			var value = ValueArrayFactory.CheckPointer<T>(pointers, ValueArray<T>.StorageName);
			var row = ValueArrayFactory.CheckPointer<int>(pointers, RowIndexName);
			var col = ValueArrayFactory.CheckPointer<int>(pointers, ColumnIndexName);

			long rowLen = pointers[RowIndexName].LengthInBytes / sizeof(int);
			long colLen = pointers[ColumnIndexName].LengthInBytes / sizeof(int);

			if (format == SparseMatrixFormat.Any)
			{
				if (rowLen == nnz && colLen == nnz) // do not know COOC or COOR
					throw new ArgumentException(Resource.FormatNotAtomic, nameof(otherInfo));
				else if (rowLen == size[0] + 1 && colLen == nnz)
					format = SparseMatrixFormat.CSR;
				else if (colLen == size[1] + 1 && rowLen == nnz)
					format = SparseMatrixFormat.CSC;
				else
					throw new ArgumentException(Resource.NotSupportedFormat, nameof(otherInfo));
			}
			else if (nnz == 0)
			{
				switch (format)
				{
					case SparseMatrixFormat.COOR:
					case SparseMatrixFormat.COOC:
						if (rowLen == colLen)
							nnz = rowLen;
						else
							throw new ArgumentException(Resource.MatrixWrongSize, nameof(pointers));
						break;
					case SparseMatrixFormat.CSR:
						if (rowLen == size[0] + 1)
							nnz = colLen;
						else
							throw new ArgumentException(Resource.MatrixWrongSize, nameof(pointers));
						break;
					case SparseMatrixFormat.CSC:
						if (colLen == size[1] + 1)
							nnz = rowLen;
						else
							throw new ArgumentException(Resource.MatrixWrongSize, nameof(pointers));
						break;
					default:
						throw new ArgumentException(Resource.FormatNotAtomic, nameof(otherInfo));
				}
			}

			switch (format)
			{
				case SparseMatrixFormat.COOR:
				case SparseMatrixFormat.COOC:
					if (rowLen != colLen || rowLen != nnz)
						throw new ArgumentException(Resource.MatrixWrongSize, nameof(pointers));
					break;
				case SparseMatrixFormat.CSR:
					if (rowLen != size[0] + 1 || colLen != nnz)
						throw new ArgumentException(Resource.MatrixWrongSize, nameof(pointers));
					break;
				case SparseMatrixFormat.CSC:
					if (colLen != size[1] + 1 || rowLen != nnz)
						throw new ArgumentException(Resource.MatrixWrongSize, nameof(pointers));
					break;
				default:
					throw new ArgumentException(Resource.FormatNotAtomic, nameof(otherInfo));
			}

			return (format, herm, value, row, col);
		}

		public ValueArray<T> CreateArray<T>(IReadOnlyList<long> size, bool onHost, IReadOnlyDictionary<string, object> otherInfo = null) where T : unmanaged, IFormattable, IEquatable<T>
		{
			var (nnz, format, herm) = Check(size, otherInfo);
			return new AbstractSparseMatrix<T>(size[0], size[1], nnz, format, onHost, herm);
		}

		public ValueArray<T> ReconstructArray<T>(IReadOnlyList<long> size, IReadOnlyDictionary<string, IStorage> pointers, IReadOnlyDictionary<string, object> otherInfo = null) where T : unmanaged, IFormattable, IEquatable<T>
		{
			var (format, herm, value, row, col) = Check<T>(size, pointers, otherInfo);
			return new AbstractSparseMatrix<T>(size[0], size[1], value, row, col, format, herm);
		}

		internal static IReadOnlyDictionary<string, IStorage> GetPointers<T>(SparseMatrix<T> mat) where T : unmanaged, IFormattable, IEquatable<T>
		{
			return new Dictionary<string, IStorage>
			{
				[ValueArray<T>.StorageName] = mat.Storage,
				[RowIndexName] = mat.RowPointer,
				[ColumnIndexName] = mat.ColumnPointer
			};
		}

		internal static IReadOnlyDictionary<string, object> GetOtherInfo<T>(SparseMatrix<T> mat) where T : unmanaged, IFormattable, IEquatable<T>
		{
			return new Dictionary<string, object>
			{
				[HermitianName] = mat.Hermitian,
				[NonZerosName] = mat.NonZero,
				[FormatName] = mat.Format
			};
		}
	}


	internal sealed class DenseTensorFactory : IArrayFactory
	{
		private const string LabelName = nameof(DenseTensor<int>.Label);

		private static IReadOnlyList<char> GetLabel(IReadOnlyDictionary<string, object> otherInfo)
		{
			if (otherInfo is null || !otherInfo.ContainsKey(LabelName))
				return null;
			if (otherInfo[LabelName] is IReadOnlyList<char> read)
				return read;
			if (otherInfo[LabelName] is char[] array)
				return array;
			if (otherInfo[LabelName] is IList<char> list)
			{
				var arr = new char[list.Count];
				list.CopyTo(arr, 0);
				return arr;
			}
			if (otherInfo[LabelName] is Newtonsoft.Json.Linq.JArray json)
				return json.ToObject<char[]>();
			return null; // cannot identify type
		}

		public ValueArray<T> CreateArray<T>(IReadOnlyList<long> size, bool onHost, IReadOnlyDictionary<string, object> otherInfo = null) where T : unmanaged, IFormattable, IEquatable<T>
		{
			if (size is null || size.Count == 0)
				throw new ArgumentNullException(nameof(size));
			return new DenseTensor<T>(size, GetLabel(otherInfo), onHost);
		}

		public ValueArray<T> ReconstructArray<T>(IReadOnlyList<long> size, IReadOnlyDictionary<string, IStorage> pointers, IReadOnlyDictionary<string, object> otherInfo = null) where T : unmanaged, IFormattable, IEquatable<T>
		{
			if (size is null || size.Count == 0)
				throw new ArgumentNullException(nameof(size));

			var data = ValueArrayFactory.CheckPointer<T>(pointers, ValueArray<T>.StorageName, size.Prod());
			var label = GetLabel(otherInfo);
			var result = new DenseTensor<T>(data, size);
			if (label != null)
				result.Label = label;
			return result;
		}

		internal static IReadOnlyDictionary<string, IStorage> GetPointers<T>(DenseTensor<T> ten) where T : unmanaged, IFormattable, IEquatable<T>
		{
			return new Dictionary<string, IStorage>
			{
				[ValueArray<T>.StorageName] = ten.Storage
			};
		}

		internal static IReadOnlyDictionary<string, object> GetOtherInfo<T>(DenseTensor<T> ten) where T : unmanaged, IFormattable, IEquatable<T>
		{
			return new Dictionary<string, object>
			{
				[LabelName] = ten.Label
			};
		}
	}
}
