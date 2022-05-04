using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

using Althea.Helpers;
using Althea.LinearAlgebra;
using Althea.Linq;
using Althea.NativeTypes;
using Althea.Storage;
using Althea.TensorAlgebra;

using ExtTen = Althea.TensorAlgebra.Dense.ExtendApiSelector;
using Ten = Althea.TensorAlgebra.Dense.BaseApiSelector;


namespace Althea.Array
{
	/// <summary>
	/// The base dense tensor class whose only storage is of type <typeparamref name="TS"/>.
	/// </summary>
	/// <typeparam name="T">Any unmanaged number as the data type</typeparam>
	/// <typeparam name="TS">The storage type used by the value storage</typeparam>
	[StructLayout(LayoutKind.Explicit)]
	public class DenseTensor<T, TS> : IDenseArray<T, TS>,
		IBaseTensor<T, DenseTensor<T, TS>>,
		ITensorUnaryOperators<T, DenseTensor<T, TS>, DenseTensor<T, TS>>,
		ITensorBinaryOperators<T, DenseTensor<T, TS>, DenseTensor<T, TS>, DenseTensor<T, TS>>
		where T : unmanaged, INumber<T>
		where TS : class, IStorage<T, TS>
	{
		#region basic
		[FieldOffset(0)]
		private readonly FixedBuffer_128<long> size;
		[FieldOffset(128 - sizeof(long))]
		private readonly int rank;
		[FieldOffset(128)]
		private readonly FixedBuffer_128<long> sizeProd;
		[FieldOffset(128 * 2 - sizeof(long))]
		private readonly long length;
		[FieldOffset(128 * 2)]
		private readonly FixedBuffer_128<long> outerSize;
		[FieldOffset(128 * 3)]
		private readonly FixedBuffer_128<long> strides;
		[FieldOffset(128 * 4 - sizeof(long))]
		private readonly long outerLength;
		[FieldOffset(128 * 4)]
		private FixedBuffer_32<char> labels;
		[FieldOffset(128 * 4 + 32)]
		private readonly TS values;

		private const byte MAX_RANK = 15;

		/// <inheritdoc/>
		public int Rank => this.rank;

		/// <inheritdoc/>
		public long Length => this.length;

		/// <summary>
		/// Get the value storage of this tensor.
		/// </summary>
		public TS Storage => this.values.MakeReference();

		/// <inheritdoc/>
		public ReadOnlySpan<long> Size => this.size.AsSpan(this.rank);

		/// <inheritdoc/>
		public ReadOnlySpan<long> SizeProd => this.sizeProd.AsSpan(this.rank + 1);

		/// <inheritdoc/>
		public ReadOnlySpan<long> OuterSize => this.outerSize.AsSpan(this.rank);

		/// <inheritdoc/>
		public ReadOnlySpan<long> Strides => this.outerSize.AsSpan(this.rank + 1);

		/// <inheritdoc/>
		public ReadOnlySpan<char> Labels
		{
			get => this.labels.AsSpan(this.rank);
			set
			{
				if (value.Length != this.rank)
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(value));
				this.labels.CopyFromSpan(value);
			}
		}

		/// <inheritdoc/>
		public char GetLabel(int index)
		{
			if (index < 0 || index >= this.rank)
				throw new ArgumentOutOfRangeException(nameof(index));
			return this.labels[index];
		}

		/// <inheritdoc/>
		public void SetLabel(int index, char label)
		{
			if (index < 0 || index >= this.rank)
				throw new ArgumentOutOfRangeException(nameof(index));
			this.labels[index] = label;
		}

		/// <inheritdoc/>
		public void SetLabels(params char[] labels!!)
		{
			if (labels.Length != this.rank)
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(labels));
			this.labels.CopyFromSpan(labels);
		}

		private DenseTensor() => this.values = TS.Empty;

		static DenseTensor<T, TS> IValueArray<T, DenseTensor<T, TS>>.Empty => new();

		bool ICheckValid.IsValid() => this.values.IsValid();

		/// <summary>
		/// Create a new <see cref="DenseVector{T, TS}"/> with given <paramref name="storage"/> and <paramref name="size"/>.
		/// </summary>
		/// <param name="storage">The value storage of the new tensor as a <typeparamref name="TS"/></param>
		/// <param name="size">The presenting size of the new tensor</param>
		/// <param name="outerSize">The actual outer size of the new tensor, default means the same as <paramref name="size"/></param>
		/// <param name="labels">The labels of all dimensions of the new tensor, default means <c>{'a', 'b', ...}</c></param>
		/// <exception cref="ArgumentException">If the sizes mismatch with each other</exception>
		/// <exception cref="NotSupportedException">If the rank is too high</exception>
		public DenseTensor(TS storage!!, ReadOnlySpan<long> size, ReadOnlySpan<long> outerSize = default, ReadOnlySpan<char> labels = default)
		{
			if (!storage.IsValid())
				throw new ArgumentNullException(nameof(storage));
			this.rank = size.Length;
			if (this.rank > MAX_RANK)
				throw new NotSupportedException();
			this.length = size.Prod();
			this.size.CopyFromSpan(size);
			size.AccumulateProd(this.sizeProd.AsSpan(this.rank + 1));
			if (!outerSize.IsEmpty)
			{
				if (outerSize.Length != this.rank)
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(outerSize));
				if (!outerSize.SequenceLargerEqualThan(size))
					throw new ArgumentException(Resources.ParameterError.InvalidValue, nameof(outerSize));
				this.outerSize.CopyFromSpan(outerSize);
				outerSize.AccumulateProd(this.strides.AsSpan(this.rank + 1));
			}
			else
			{
				this.outerSize.CopyFromSpan(size);
				this.strides.CopyFromSpan(this.SizeProd);
			}
			this.outerLength = this.Strides[^1];
			if (!labels.IsEmpty)
			{
				if (labels.Length != this.rank)
					throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(labels));
				if (labels.DistinctCount() != this.rank)
					throw new ArgumentException(Resources.ParameterError.DuplicateValue, nameof(labels));
				this.labels.CopyFromSpan(labels);
			}
			else
			{
				this.labels.AsSpan(this.rank).FillWithLabel();
			}
			long valueLen = this.Strides[^2] * this.Size[^1];
			if (storage.Length < valueLen)
				throw new ArgumentException(Resources.ParameterError.WrongSize, nameof(storage));
			if (storage.Length > valueLen)
				storage = storage.MakeReference(0, valueLen);
			this.values = storage.AddToManager();
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			this.values.SafeDispose();
			GC.SuppressFinalize(this);
		}
		#endregion

		#region equality
		/// <inheritdoc/>
		public bool Equals(DenseTensor<T, TS>? other) => ReferenceEquals(this, other) || (other is not null && this.size == other.size && this.outerSize == other.outerSize && this.values == other.values);

		/// <summary>
		/// Equality operator
		/// </summary>
		public static bool operator ==(DenseTensor<T, TS> left, DenseTensor<T, TS> right) => left.Equals(right);

		/// <summary>
		/// Inequality operator
		/// </summary>
		public static bool operator !=(DenseTensor<T, TS> left, DenseTensor<T, TS> right) => !left.Equals(right);

		/// <inheritdoc/>
		public override bool Equals(object? obj) => this.Equals(obj as DenseTensor<T, TS>);

		/// <inheritdoc/>
		public override int GetHashCode() => HashCode.Combine(this.values, this.size, this.outerSize);
		#endregion

		#region element indexing
		/// <inheritdoc/>
		public T this[ReadOnlySpan<long> indices]
		{
			get
			{
				return (this.values + IBaseTensor<T, DenseTensor<T, TS>>.CheckIndex(this, indices)).ToManaged<T, TS>();
			}
			set
			{
				(this.values + IBaseTensor<T, DenseTensor<T, TS>>.CheckIndex(this, indices)).FromManaged(value);
			}
		}
		#endregion

		#region range indexing
		/// <inheritdoc/>
		public DenseTensor<T, TS> GetSlice(ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths)
		{
			var storage = this.values + IBaseTensor<T, DenseTensor<T, TS>>.CheckRange(this, offsets, lengths);
			return new(storage, lengths, this.OuterSize);
		}

		/// <inheritdoc/>
		public void GetSlice(ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths, DenseTensor<T, TS> overwrite)
		{
			var storage = this.values + IBaseTensor<T, DenseTensor<T, TS>>.CheckRange(this, offsets, lengths, overwrite);
			Ten.Permute<T, TS, TS>(new(storage, lengths, this.OuterSize, this.Strides), new(overwrite), stackalloc int[this.rank].FillWithRange(0));
		}

		/// <inheritdoc/>
		public void CopyTo(DenseTensor<T, TS> destination)
		{
			if (!this.Size.SequenceEqual(destination.Size))
				throw new ArgumentException(Resources.ParameterError.NotSameSize, nameof(destination));
			Ten.Permute<T, TS, TS>(new(this), new(destination), stackalloc int[this.rank].FillWithRange(0));
		}

		/// <inheritdoc/>
		public void SetSlice(ReadOnlySpan<long> offsets, ReadOnlySpan<long> lengths, DenseTensor<T, TS> value)
		{
			var storage = this.values + IBaseTensor<T, DenseTensor<T, TS>>.CheckRange(this, offsets, lengths, value);
			Ten.Permute<T, TS, TS>(new(value), new(storage, lengths, this.OuterSize, this.Strides), stackalloc int[this.rank].FillWithRange(0));
		}
		#endregion

		#region first few dimensions indexing
		/// <inheritdoc/>
		public DenseTensor<T, TS> GetFirstDims(int n, ReadOnlySpan<long> restIndices, ReadOnlySpan<long> offsets = default, ReadOnlySpan<long> lengths = default)
		{
			Span<long> allOffsets = stackalloc long[this.rank], allLengths = stackalloc long[this.rank];
			var storage = this.values + IBaseTensor<T, DenseTensor<T, TS>>.CheckFirstDims(this, n, restIndices, offsets, lengths, allOffsets, allLengths);
			return new(storage, allLengths[..n], this.OuterSize[..n]);
		}

		/// <inheritdoc/>
		public void GetFirstDims(int n, ReadOnlySpan<long> restIndices, DenseTensor<T, TS> overwrite, ReadOnlySpan<long> offsets = default, ReadOnlySpan<long> lengths = default)
		{
			Span<long> allOffsets = stackalloc long[this.rank], allLengths = stackalloc long[this.rank];
			var storage = this.values + IBaseTensor<T, DenseTensor<T, TS>>.CheckFirstDims(this, n, restIndices, offsets, lengths, allOffsets, allLengths, overwrite);
			Ten.Permute<T, TS, TS>(new(storage, allLengths[..n], this.OuterSize[..n], this.Strides[..(n + 1)]), new(overwrite), stackalloc int[n].FillWithRange(0));
		}

		/// <inheritdoc/>
		public void SetFirstDims(int n, ReadOnlySpan<long> restIndices, DenseTensor<T, TS> value, ReadOnlySpan<long> offsets = default, ReadOnlySpan<long> lengths = default)
		{
			Span<long> allOffsets = stackalloc long[this.rank], allLengths = stackalloc long[this.rank];
			var storage = this.values + IBaseTensor<T, DenseTensor<T, TS>>.CheckFirstDims(this, n, restIndices, offsets, lengths, allOffsets, allLengths, value);
			Ten.Permute<T, TS, TS>(new(value), new(storage, allLengths[..n], this.OuterSize[..n], this.Strides[..(n + 1)]), stackalloc int[n].FillWithRange(0));
		}
		#endregion

		#region point-wise operations
		/// <inheritdoc/>
		public void FillWith(T value) => ExtTen.OperationBinaryScalar<T, TS>(BinaryScalarOperation.Fill, new(this), value);

		/// <inheritdoc/>
		public void AddScalar(T value) => ExtTen.OperationBinaryScalar<T, TS>(BinaryScalarOperation.Add, new(this), value);

		/// <inheritdoc/>
		public void Scale(T value) => ExtTen.OperationBinaryScalar<T, TS>(BinaryScalarOperation.Multiply, new(this), value);

		/// <inheritdoc/>
		public void Conjugate() => Ten.Permute<T, TS, TS>(new(this, UnaryOperation.Conjugate), new(this), stackalloc int[this.rank].FillWithRange(0));
		#endregion

		#region simple aggregation operations
		/// <inheritdoc/>
		public T Sum() => ExtTen.FullReduce<T, TS>(ReduceOperation.Add, new(this));

		/// <inheritdoc/>
		public T AbsSum() => ExtTen.FullReduce<T, TS>(ReduceOperation.AddAbsolute, new(this));

		/// <inheritdoc/>
		public T Norm() => ExtTen.FullReduce<T, TS>(ReduceOperation.Norm, new(this));

		/// <inheritdoc/>
		public T ValueWithMaxAbs() => ExtTen.FullReduce<T, TS>(ReduceOperation.AbsoluteMaximum, new(this));

		/// <inheritdoc/>
		public T ValueWithMinAbs() => ExtTen.FullReduce<T, TS>(ReduceOperation.AbsoluteMininum, new(this));
		#endregion

		#region operators
		/// <inheritdoc/>
		public static DenseTensor<T, TS> operator ^(DenseTensor<T, TS> tensor, TensorOrder order) => DenseOperation<T, TS>.Permute(tensor, order, T.One);

		/// <inheritdoc/>
		public static DenseTensor<T, TS> operator *(DenseTensor<T, TS> tensor, T scalar) => DenseOperation<T, TS>.Permute(tensor, TensorOrder.Identity, scalar);

		/// <inheritdoc/>
		public static DenseTensor<T, TS> operator *(T scalar, DenseTensor<T, TS> tensor!!) => tensor * scalar;

		/// <inheritdoc/>
		public static DenseTensor<T, TS> operator -(DenseTensor<T, TS> tensor) => tensor * (-T.One);

		/// <inheritdoc/>
		public static DenseTensor<T, TS> operator /(DenseTensor<T, TS> tensor, T scalar) => tensor * (T.One / scalar);

		/// <inheritdoc/>
		public static DenseTensor<T, TS> operator *(DenseTensor<T, TS> left, DenseTensor<T, TS> right) => DenseOperation<T, TS>.Contract(left, UnaryOperation.Identity, right, UnaryOperation.Identity, T.One);

		/// <inheritdoc/>
		public static DenseTensor<T, TS> operator +(DenseTensor<T, TS> left, DenseTensor<T, TS> right) => DenseOperation<T, TS>.TensorsBinaryOperation(left, TensorOrder.Identity, UnaryOperation.Identity, T.One, right, TensorOrder.Identity, UnaryOperation.Identity, T.One, BinaryOperation.Add);

		/// <inheritdoc/>
		public static DenseTensor<T, TS> operator -(DenseTensor<T, TS> left, DenseTensor<T, TS> right) => DenseOperation<T, TS>.TensorsBinaryOperation(left, TensorOrder.Identity, UnaryOperation.Identity, T.One, right, TensorOrder.Identity, UnaryOperation.Negate, T.One, BinaryOperation.Add);
		#endregion

		#region conversion and clone
		/// <inheritdoc/>
		public DenseTensor<T, TS> CreateAlike() => new(this.values.ResizeAlike(this.length), this.Size);

		/// <summary>
		/// Copy the values from this dense tensor to a new <typeparamref name="TS"/> without stride.
		/// </summary>
		/// <returns>The created compact tensor's storage as a <typeparamref name="TS"/></returns>
		public TS ToCompact()
		{
			var compact = this.values.ResizeAlike(this.length);
			try
			{
				Ten.Permute<T, TS, TS>(new(this), new(compact, this.Size), stackalloc int[this.rank].FillWithRange(0));
				return compact;
			}
			catch (Exception)
			{
				compact.Dispose();
				throw;
			}
		}
		#endregion

		#region serialization
		private record struct Repr(TS Values, long[] Size, long[] OuterSize);
		private static JsonSerializerOptions JsonOptions => new()
		{
			Converters = { TS.JsonConverter },
			WriteIndented = true,
		};

		/// <inheritdoc/>
		public string JsonSerialize()
		{
			return JsonSerializer.Serialize<Repr>(new(this.values, this.Size.ToArray(), this.outerSize.ToArray()), JsonOptions);
		}

		/// <inheritdoc/>
		public static DenseTensor<T, TS> JsonDeserialize(string json!!)
		{
			var repr = JsonSerializer.Deserialize<Repr>(json, JsonOptions);
			return new(repr.Values, repr.Size, repr.OuterSize);
		}
		#endregion

		#region string
		static string IMainPropertyFormattable<DenseTensor<T, TS>>.StringMain => nameof(DenseTensor<T, TS>);

		static IEnumerable<string> IMainPropertyFormattable<DenseTensor<T, TS>>.PropertyNames => new[] { "DataType", "Values", "Size", "OuterSize" };

		IEnumerable<object?> IMainPropertyFormattable<DenseTensor<T, TS>>.PropertyValues => new object[] { Unmanaged<T>.DataType, this.values, "{" + this.Size.SpanJoin('x') + "}", "{" + this.OuterSize.SpanJoin('x') + "}" };

		/// <inheritdoc/>
		public override string ToString() => IMainPropertyFormattable<DenseTensor<T, TS>>.ToString(this);

		private void GetSizePos(long offset, Span<long> pos)
		{
			int atRank = this.rank - pos.Length;
			for (int i = this.rank - 1, j = pos.Length - 1; i >= atRank; i--, j--)
			{
				pos[j] = offset / this.sizeProd[i];
				offset %= this.sizeProd[i];
			}
		}
		private void GetOuterSizePos(long offset, Span<long> pos)
		{
			int atRank = this.rank - pos.Length;
			for (int i = this.rank - 1, j = pos.Length - 1; i >= atRank; i--, j--)
			{
				pos[j] = offset / this.strides[i];
				offset %= this.strides[i];
			}
		}

		/// <inheritdoc/>
		public string Print(PrintSettings? settings = null)
		{
			var ps = settings ?? Settings.PrintSetting;
			if (this.rank == 1)
			{
				using var vec = new DenseVector<T, TS>(this.Storage, this.length);
				return vec.Print(settings);
			}
			if (this.rank == 2)
			{
				using var mat = new DenseMatrix<T, TS>(this.Storage, this.size[0], this.size[1], this.outerSize[0]);
				return mat.Print(settings);
			}
			if (ps.MatrixFormTensor)
			{
				// get truncated size
				int matrixMaxRows = ps.MatrixRow, matrixMaxCols = ps.MatrixColumn;
				Span<long> truncateSize = stackalloc long[this.rank];
				this.size.CopyToSpan(truncateSize);
				int d = truncateSize.IndexOf(static s => s > 1);
				long rows = truncateSize[d];
				truncateSize[d] = Math.Min(matrixMaxRows, truncateSize[d]);
				long ld = truncateSize[d];
				d = truncateSize[(d + 1)..].IndexOf(static s => s > 1) + d + 1;
				long cols = truncateSize[d];
				truncateSize[d] = Math.Min(matrixMaxCols, truncateSize[d]);
				long matrixLength = rows * cols;
				// reduce matrix size
				matrixMaxRows = (int)Math.Ceiling(Math.Sqrt(matrixMaxRows));
				matrixMaxCols = (int)Math.Ceiling(Math.Sqrt(matrixMaxCols));
				ps = ps with { MatrixRow = matrixMaxRows, MatrixColumn = matrixMaxCols };
				d = truncateSize.IndexOf(static s => s > 1);
				truncateSize[d] = Math.Min(matrixMaxRows, truncateSize[d]);
				ld = truncateSize[d];
				d = truncateSize[(d + 1)..].IndexOf(static s => s > 1) + d + 1;
				truncateSize[d] = Math.Min(matrixMaxCols, truncateSize[d]);
				matrixLength = rows * cols;
				// print
				return TensorPrinter.Print(this.Size, this.OuterSize, this.Strides, this.values, truncateSize, rows, cols, ld, ps);
			}
			else
			{
				long nMatrices = this.Size[2..].Prod();
				int length = Math.Min((int)nMatrices, ps.ArrayLength);
				StringBuilder sb = new();
				Span<long> restInds = stackalloc long[this.rank - 2];
				for (int i = 0; i < length; i++)
				{
					GetSizePos(i, restInds);
					using var part = this.GetFirstDims(2, restInds, default, default);
					sb.AppendLine($"Tensor[.., .., {restInds.SpanJoin(", ")}] = ");
					sb.AppendLine(part.Print());
				}
				return sb.ToString() + (length == nMatrices ? "" : string.Format(Resources.Print.MoreStored, nMatrices - length));
			}
		}

		private readonly ref struct TensorPrinter
		{
			private readonly TS storage;

			private readonly ReadOnlySpan<long> size;

			private readonly ReadOnlySpan<long> outerSize;

			private readonly ReadOnlySpan<long> outerSizeProd;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private TensorPrinter(ReadOnlySpan<long> size, ReadOnlySpan<long> outerSize, ReadOnlySpan<long> outerSizeProd, TS storage)
			{
				this.storage = storage;
				this.size = size;
				this.outerSize = outerSize;
				this.outerSizeProd = outerSizeProd;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private TensorPrinter(TensorPrinter parent, long rowIndex, long colIndex)
			{
				this.size = parent.size[..^2];
				this.outerSize = parent.outerSize[..^2];
				this.outerSizeProd = parent.outerSizeProd[..^2];
				this.storage = parent.storage + (parent.outerSizeProd[^2] * (rowIndex + parent.outerSize[^2] * colIndex));
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private static void AppendRow(StringBuilder sb, string currentRow, string prefix, string postfix, bool lastRow, long moreRows)
			{
				if (lastRow)
				{
					if (moreRows <= 0)
					{
						sb.Append(currentRow);
						return;
					}
					sb.AppendLine(currentRow).Append(prefix).AppendFormat(Resources.Print.MoreRows, moreRows);
				}
				else
				{
					sb.AppendLine(currentRow);
					int find = currentRow.LastIndexOf(Environment.NewLine, sb.Length - 2);
					int lineWidth = sb.Length - find - Environment.NewLine.Length * 2 - postfix.Length;
					sb.Append(prefix.PadRight(lineWidth)).AppendLine(postfix);
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal static string Print(ReadOnlySpan<long> size, ReadOnlySpan<long> outerSize, ReadOnlySpan<long> outerSizeProd, TS storage, ReadOnlySpan<long> matrixSize, long rows, long cols, long ld, PrintSettings settings, string? prefix = null, string? postfix = null)
			{
				int rank = size.Length;
				bool topLayerIsVec = rank % 2 == 1;
				long matrixLength = matrixSize.Prod();
				prefix += "|"; postfix = "|" + postfix;
				TensorPrintCommonInfo info = new(outerSize, matrixSize, rows, cols, ld, matrixLength, settings);
				if (topLayerIsVec)
				{
					StringBuilder sb = new();
					string subPrefix = prefix + "|", subPostfix = "|" + postfix;
					long crows = size[0], lastOuterSizeProd = outerSizeProd[^1];
					int nrows = (int)Math.Min(crows, settings.MatrixRow);
					ReadOnlySpan<long> vecSize = size[..^1], vecOuterSize = outerSize[..^1], vecOuterSizeProd = outerSizeProd[..^1];
					for (int i = 0; i < nrows; i++)
					{
						TensorPrinter current = new(vecSize, vecOuterSize, vecOuterSizeProd, storage + lastOuterSizeProd * i);
						string currentRow = Print(current, info, subPrefix, subPostfix);
						AppendRow(sb, currentRow, prefix, postfix, lastRow: i == nrows - 1, moreRows: 0);
					}
					return sb.ToString();
				}
				else
				{
					var tensor = new TensorPrinter(size, outerSize, outerSizeProd, storage);
					return Print(tensor, info, prefix, postfix);
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
			private static unsafe string Print(TensorPrinter tensor, TensorPrintCommonInfo info, string prefix, string postfix)
			{
				if (tensor.size.Length == 2)
				{
					using var temp = ((int)info.MatrixNowLength).CheckStackLimit<T>();
					Span<T> values = temp.IsEmpty ? stackalloc T[((int)info.MatrixNowLength)] : temp.Data;
					fixed (T* tt = values)
					{
						var mp = new ManagedPureStorage<T>(new(new(tt), values.Length * sizeof(T)));
						Ten.Permute<T, TS, ManagedPureStorage<T>>(new(tensor.storage, info.MatrixSize, info.OuterSize, stackalloc long[] { 1, info.MatrixNowLD }), new(mp, info.MatrixSize), stackalloc int[info.MatrixSize.Length].FillWithRange(0));
					}
					return values.ToMatrixString((int)info.MatrixOrgRows, false, info.MatrixSize[1] - info.MatrixOrgCols, info.Settings.Precision, prefix, postfix);
				}
				// else
				StringBuilder sb = new();
				long rows = tensor.size[0], cols = tensor.size[1];
				int nrows = (int)Math.Min(rows, info.Settings.MatrixRow), ncols = (int)Math.Min(cols, info.Settings.MatrixColumn);
				string moreElem = cols > ncols ? string.Format(Resources.Print.RowMore + postfix, cols - ncols) : postfix;
				string[] subMatsCurrentRow = new string[ncols];
				for (int i = 0; i < nrows; i++)
				{
					for (int j = 0; j < ncols; j++)
					{
						TensorPrinter current = new(tensor, i, j);
						subMatsCurrentRow[j] = Print(current, info, "|", "|");
					}
					string currentRow = subMatsCurrentRow.MultilineConcat(prefix, "|  |", moreElem);
					AppendRow(sb, currentRow, prefix, postfix, lastRow: i == nrows - 1, moreRows: rows - nrows);
				}
				return sb.ToString();
			}
		}

		private sealed class TensorPrintCommonInfo
		{
			private readonly FixedBuffer_128<long> outerSize = default, matrixSize = default;

			private readonly long rows, cols, ld, length;

			private readonly int rank;

			private readonly PrintSettings settings;


			internal ReadOnlySpan<long> OuterSize => this.outerSize.AsSpan(this.rank);

			internal ReadOnlySpan<long> MatrixSize => this.matrixSize.AsSpan(this.rank);

			internal long MatrixOrgRows => this.rows;
			
			internal long MatrixOrgCols => this.cols;
			
			internal long MatrixNowLD => this.ld;

			internal long MatrixNowLength => this.length;

			internal PrintSettings Settings => this.settings;

			internal TensorPrintCommonInfo(ReadOnlySpan<long> outerSize, ReadOnlySpan<long> matrixSize, long rows, long cols, long ld, long matrixLength, PrintSettings settings)
			{
				this.outerSize.CopyFromSpan(outerSize); this.matrixSize.CopyFromSpan(matrixSize);
				this.rank = outerSize.Length;
				this.rows = rows; this.cols = cols; this.ld = ld; this.length = matrixLength;
				this.settings = settings;
			}
		}
		#endregion
	}
}
